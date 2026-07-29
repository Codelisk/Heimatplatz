using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Debug.Services;
using Heimatplatz.Maui.Offline;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.DocumentDb;
using Shiny.Mediator;
using Shiny.Mediator.Infrastructure;

namespace Heimatplatz.Maui.Features.Properties.Sync;

/// <summary>
/// Delta-Sync fuer Immobilien-Daten: fragt periodisch GET /api/properties/changes ab
/// und bringt die lokalen Caches gezielt auf Stand, statt ganze Listen neu zu laden.
///
/// - Geaenderte Immobilien werden in allen gecachten Listen-Antworten in-place ersetzt.
/// - Geloeschte Immobilien werden aus Listen entfernt; Detail-Caches werden verworfen.
/// - Fuer geaenderte Immobilien mit gecachtem Detail wird nur dieses eine Detail
///   nachgeladen (ForceCacheRefresh aktualisiert Cache und Offline-Store).
/// - Neue Immobilien lassen sich client-seitig nicht korrekt in gefilterte/sortierte
///   Listen einordnen (Filterlogik gehoert dem Backend) - die Listen-Caches werden
///   dafuer als veraltet markiert und beim naechsten Zugriff im Hintergrund erneuert.
///
/// Nach jedem Sync mit Aenderungen wird <see cref="PropertyDataSyncedEvent"/> publiziert.
/// </summary>
[Singleton]
public sealed class PropertySyncService(
    IMediator mediator,
    ICacheService cache,
    IStorageService storage,
    IDocumentStore documentStore,
    IContractKeyProvider contractKeyProvider,
    IInternetService internet,
    IConfiguration configuration,
    CacheStalenessRegistry stalenessRegistry,
    ILogger<PropertySyncService> logger
) : IDisposable
{
    private const string WatermarkPreferenceKeyPrefix = "property-sync.watermark:";
    private const string CacheCategory = "Cache";
    private const string OfflineCategory = "Offline";

    private static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(60);

    private static readonly string[] ListRequestTypeNames =
    [
        typeof(GetPropertiesHttpRequest).FullName!,
        typeof(GetUserPropertiesHttpRequest).FullName!,
        typeof(GetUserFavoritesHttpRequest).FullName!,
        typeof(GetUserBlockedHttpRequest).FullName!
    ];

    private static readonly string DetailRequestTypeName = typeof(GetPropertyByIdHttpRequest).FullName!;

    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private CancellationTokenSource? _loopCts;

    /// <summary>
    /// Startet den periodischen Sync (idempotent). Laeuft solange die App lebt;
    /// einzelne Fehlschlaege werden geloggt und beim naechsten Intervall erneut versucht.
    /// </summary>
    public void Start()
    {
        if (_loopCts is not null)
            return;

        var cts = new CancellationTokenSource();
        if (Interlocked.CompareExchange(ref _loopCts, cts, null) is not null)
        {
            cts.Dispose();
            return;
        }

        _ = Task.Run(() => RunLoopAsync(cts.Token));
    }

    /// <summary>Loest sofort einen Sync aus (z.B. bei App-Resume)</summary>
    public void TriggerSyncNow()
        => Task.Run(() => SyncAsync(CancellationToken.None)).RunInBackground(logger);

    private async Task RunLoopAsync(CancellationToken ct)
    {
        // Erster Lauf direkt beim Start
        await SyncSafeAsync(ct).ConfigureAwait(false);

        using var timer = new PeriodicTimer(SyncInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                await SyncSafeAsync(ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // App-Ende
        }
    }

    private async Task SyncSafeAsync(CancellationToken ct)
    {
        try
        {
            await SyncAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Immobilien-Delta-Sync fehlgeschlagen; naechster Versuch im Intervall");
        }
    }

    /// <summary>
    /// Fuehrt einen einzelnen Delta-Sync durch. Ohne Internet oder bei bereits
    /// laufendem Sync passiert nichts.
    /// </summary>
    public async Task SyncAsync(CancellationToken ct)
    {
        if (!internet.IsAvailable)
            return;

        if (!await _syncLock.WaitAsync(0, ct).ConfigureAwait(false))
            return;

        try
        {
            var watermarkKey = GetWatermarkPreferenceKey();
            var since = LoadWatermark(watermarkKey);

            var (_, response) = await mediator.Request(
                    new GetPropertyChangesHttpRequest { Since = since },
                    ct,
                    ctx => ctx.BypassExceptionHandlingEnabled = true)
                .ConfigureAwait(false);

            if (response is null)
                return;

            if (response.FullResyncRequired)
            {
                await FlushAllPropertyCachesAsync(ct).ConfigureAwait(false);
                SaveWatermark(watermarkKey, response.Watermark);

                // Beim allerersten Lauf gibt es noch keinen Stand, der veraltet sein koennte -
                // Event nur publizieren, wenn tatsaechlich ein alter Stand verworfen wurde
                if (since is not null)
                {
                    await PublishAsync(new PropertyDataSyncedEvent([], [], [], FullResync: true), ct)
                        .ConfigureAwait(false);
                }

                logger.LogInformation("Immobilien-Sync: Voll-Refresh (Since={Since})", since);
                return;
            }

            if (response.Changes is not { Count: > 0 })
            {
                SaveWatermark(watermarkKey, response.Watermark);
                return;
            }

            var deletedIds = response.Changes
                .Where(c => c.ChangeType == "Deleted")
                .Select(c => c.PropertyId)
                .ToHashSet();

            var changedProperties = response.Changes
                .Where(c => c.Property is not null)
                .ToDictionary(c => c.PropertyId, c => c.Property!);

            var createdIds = response.Changes
                .Where(c => c.ChangeType == "Created")
                .Select(c => c.PropertyId)
                .ToList();

            // 1) Gecachte Listen-Antworten in-place patchen (alle Benutzer-Scopes)
            await PatchListCachesAsync<GetPropertiesResponse>(
                ListRequestTypeNames[0], changedProperties, deletedIds,
                r => r.Properties, (r, delta) => r.Total = Math.Max(0, r.Total + delta), ct).ConfigureAwait(false);
            await PatchListCachesAsync<GetUserPropertiesResponse>(
                ListRequestTypeNames[1], changedProperties, deletedIds,
                r => r.Properties, (r, delta) => r.Total = Math.Max(0, r.Total + delta), ct).ConfigureAwait(false);
            await PatchListCachesAsync<GetUserFavoritesResponse>(
                ListRequestTypeNames[2], changedProperties, deletedIds,
                r => r.Properties, (r, delta) => r.Total = Math.Max(0, r.Total + delta), ct).ConfigureAwait(false);
            await PatchListCachesAsync<GetUserBlockedResponse>(
                ListRequestTypeNames[3], changedProperties, deletedIds,
                r => r.Properties, (r, delta) => r.Total = Math.Max(0, r.Total + delta), ct).ConfigureAwait(false);

            // 2) Detail-Caches: Geloeschte verwerfen, geaenderte gezielt nachladen
            await UpdateDetailCachesAsync(changedProperties.Keys.ToHashSet(), deletedIds, ct).ConfigureAwait(false);

            // 3) Neue Immobilien: Listen-Caches als veraltet markieren, damit der naechste
            //    Zugriff die Listen im Hintergrund erneuert (Filter-Einordnung macht das Backend)
            if (createdIds.Count > 0)
                stalenessRegistry.MarkStale(ListRequestTypeNames);

            SaveWatermark(watermarkKey, response.Watermark);

            logger.LogInformation(
                "Immobilien-Sync: {Created} neu, {Updated} geaendert, {Deleted} geloescht",
                createdIds.Count,
                changedProperties.Count - createdIds.Count,
                deletedIds.Count);

            await PublishAsync(
                    new PropertyDataSyncedEvent(
                        changedProperties.Values.ToList(),
                        createdIds,
                        deletedIds.ToList(),
                        FullResync: false),
                    ct)
                .ConfigureAwait(false);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task PatchListCachesAsync<TResponse>(
        string requestTypeFullName,
        Dictionary<Guid, PropertyListItemDto> changed,
        HashSet<Guid> deleted,
        Func<TResponse, List<PropertyListItemDto>?> getItems,
        Action<TResponse, int> adjustTotal,
        CancellationToken ct)
    {
        foreach (var key in await GetStoredKeysAsync(CacheCategory, requestTypeFullName, ct).ConfigureAwait(false))
        {
            var entry = await cache.Get<TResponse>(key, ct).ConfigureAwait(false);
            var items = entry is null ? null : getItems(entry.Value);
            if (items is null)
                continue;

            var modified = false;

            var removed = items.RemoveAll(p => deleted.Contains(p.Id));
            if (removed > 0)
            {
                adjustTotal(entry!.Value, -removed);
                modified = true;
            }

            for (var i = 0; i < items.Count; i++)
            {
                if (changed.TryGetValue(items[i].Id, out var fresh))
                {
                    items[i] = fresh;
                    modified = true;
                }
            }

            if (!modified)
                continue;

            await cache.Set(key, entry!.Value, cancellationToken: ct).ConfigureAwait(false);

            // Offline-Duplikat entfernen statt patchen: der Cache lebt dauerhaft in derselben
            // SQLite-Datenbank und wird von der LocalFirst-Middleware bevorzugt ausgeliefert;
            // der Offline-Eintrag entsteht beim naechsten erfolgreichen Abruf neu.
            await storage.Remove(OfflineCategory, key, cancellationToken: ct).ConfigureAwait(false);
        }
    }

    private async Task UpdateDetailCachesAsync(
        HashSet<Guid> changedIds,
        HashSet<Guid> deletedIds,
        CancellationToken ct)
    {
        foreach (var key in await GetStoredKeysAsync(CacheCategory, DetailRequestTypeName, ct).ConfigureAwait(false))
        {
            var entry = await cache.Get<GetPropertyByIdResponse>(key, ct).ConfigureAwait(false);
            var propertyId = entry?.Value?.Property?.Id;
            if (propertyId is not { } id)
                continue;

            if (deletedIds.Contains(id))
            {
                await RemoveCachedAsync(key, ct).ConfigureAwait(false);
                continue;
            }

            if (!changedIds.Contains(id))
                continue;

            var currentScopeKey = contractKeyProvider.GetContractKey(new GetPropertyByIdHttpRequest { Id = id });
            if (!string.Equals(key, currentScopeKey, StringComparison.Ordinal))
            {
                // Eintrag eines anderen Benutzer-Scopes: verwerfen statt fuer fremden
                // Kontext nachzuladen - er entsteht bei Bedarf neu
                await RemoveCachedAsync(key, ct).ConfigureAwait(false);
                continue;
            }

            try
            {
                // Nur dieses eine Detail frisch laden; ForceCacheRefresh laesst die
                // Middleware Cache und Offline-Store neu schreiben
                await mediator.Request(
                        new GetPropertyByIdHttpRequest { Id = id },
                        ct,
                        ctx =>
                        {
                            ctx.ForceCacheRefresh();
                            ctx.BypassExceptionHandlingEnabled = true;
                        })
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Alter Stand bleibt nutzbar; LocalFirst erneuert ihn beim naechsten Zugriff
                logger.LogDebug(ex, "Detail-Refresh fuer {PropertyId} fehlgeschlagen", id);
            }
        }
    }

    private async Task FlushAllPropertyCachesAsync(CancellationToken ct)
    {
        string[] typeNames = [.. ListRequestTypeNames, DetailRequestTypeName];
        foreach (var typeName in typeNames)
        {
            foreach (var key in await GetStoredKeysAsync(CacheCategory, typeName, ct).ConfigureAwait(false))
                await RemoveCachedAsync(key, ct).ConfigureAwait(false);

            foreach (var key in await GetStoredKeysAsync(OfflineCategory, typeName, ct).ConfigureAwait(false))
                await storage.Remove(OfflineCategory, key, cancellationToken: ct).ConfigureAwait(false);
        }
    }

    private async Task RemoveCachedAsync(string key, CancellationToken ct)
    {
        await cache.Remove(key, cancellationToken: ct).ConfigureAwait(false);
        await storage.Remove(OfflineCategory, key, cancellationToken: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Ermittelt alle gespeicherten Schluessel einer Kategorie, die zum Request-Typ gehoeren.
    /// Der Typname ist Bestandteil des Contract-Keys (siehe UserScopedContractKeyProvider).
    /// </summary>
    private async Task<List<string>> GetStoredKeysAsync(string category, string requestTypeFullName, CancellationToken ct)
    {
        var marker = $"|{requestTypeFullName}|";
        var records = await documentStore
            .Query(OfflineStorageJsonContext.Default.MediatorStorageRecord)
            .Where(x => x.Category == category)
            .ToList(ct)
            .ConfigureAwait(false);

        return records
            .Where(x => x.Key.Contains(marker, StringComparison.Ordinal))
            .Select(x => x.Key)
            .ToList();
    }

    private Task PublishAsync(PropertyDataSyncedEvent @event, CancellationToken ct)
        => mediator.Publish(@event, ct);

    // --- Watermark-Verwaltung (pro API-Endpunkt, ueberlebt App-Neustarts) ---

    private string GetWatermarkPreferenceKey()
    {
        var endpoint = configuration[ApiEndpoints.MediatorHttpConfigKey] ?? string.Empty;
        var endpointHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(endpoint)))[..12];
        return WatermarkPreferenceKeyPrefix + endpointHash;
    }

    private static string? LoadWatermark(string preferenceKey)
    {
        var raw = Preferences.Default.Get<string?>(preferenceKey, null);
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        // Aeltere App-Staende haben "+00:00" gespeichert - beim Lesen auf die
        // URL-sichere Z-Form normalisieren, sonst bliebe der Delta-Sync bis zum
        // naechsten Voll-Refresh kaputt (siehe FormatWatermark)
        return DateTimeOffset.TryParse(
            raw,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var parsed)
            ? FormatWatermark(parsed)
            : null;
    }

    private static void SaveWatermark(string preferenceKey, DateTimeOffset watermark)
        => Preferences.Default.Set(preferenceKey, FormatWatermark(watermark));

    /// <summary>
    /// ISO-8601 in UTC mit "Z" statt "+00:00". Bewusst nicht das Roundtrip-Format "O":
    /// der generierte Shiny-HTTP-Client haengt Query-Werte unkodiert an die URL, ein "+"
    /// kaeme serverseitig als Leerzeichen an. Der Server koennte Since dann nicht parsen
    /// und wuerde bei jedem Sync einen Voll-Refresh melden (Pille "Neue Inserate" bei
    /// jedem App-Start, Caches werden dabei jedes Mal verworfen).
    ///
    /// Bewusst string statt DateTimeOffset im Contract: DateTimeOffset-Query-Parameter
    /// serialisiert der Generator zusaetzlich kulturabhaengig.
    /// </summary>
    private static string FormatWatermark(DateTimeOffset value)
        => value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);

    public void Dispose()
    {
        _loopCts?.Cancel();
        _loopCts?.Dispose();
        _syncLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
