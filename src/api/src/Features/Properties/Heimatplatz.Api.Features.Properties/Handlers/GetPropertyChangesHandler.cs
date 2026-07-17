using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Delta-Sync-Endpoint fuer Client-Caches: dedupliziert das PropertyChange-Journal
/// pro Immobilie und liefert fuer Created/Updated die aktuellen Listendaten mit,
/// damit Clients nur die tatsaechlich geaenderten Immobilien nachladen muessen.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/properties")]
public class GetPropertyChangesHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration
) : IRequestHandler<GetPropertyChangesRequest, GetPropertyChangesResponse>
{
    /// <summary>
    /// Journal-Eintraege aelter als diese Spanne werden geloescht (PropertyChangeRetentionWorker).
    /// Clients mit aelterem Since bekommen FullResyncRequired.
    /// </summary>
    public static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);

    /// <summary>
    /// Sicherheitsabstand beim Vorruecken des Wasserzeichens ohne neue Aenderungen:
    /// laufende Transaktionen koennen Journal-Eintraege mit leicht aelterem CreatedAt
    /// erst nach unserer Abfrage committen.
    /// </summary>
    private static readonly TimeSpan WatermarkSafetyMargin = TimeSpan.FromSeconds(10);

    [MediatorHttpGet("/changes", OperationId = "GetPropertyChanges")]
    public async Task<GetPropertyChangesResponse> Handle(
        GetPropertyChangesRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        // Ohne Since (Erstlauf), bei unlesbarem Wert oder mit Stand ausserhalb der
        // Aufbewahrung ist kein vollstaendiges Delta moeglich -> Client laedt komplett neu
        if (!DateTimeOffset.TryParse(
                request.Since,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var since) ||
            since < now - RetentionPeriod)
        {
            return new GetPropertyChangesResponse
            {
                Watermark = now - WatermarkSafetyMargin,
                FullResyncRequired = true,
                Changes = []
            };
        }

        // SQLite (lokale Entwicklung) kann DateTimeOffset-Vergleiche nicht in SQL uebersetzen -
        // dort in-memory filtern (Journal ist durch Retention klein); Postgres filtert in SQL
        var journal = dbContext.Set<PropertyChange>();
        var relevant = IsSqlite(dbContext)
            ? (await journal.ToListAsync(cancellationToken)).Where(c => c.CreatedAt > since).ToList()
            : await journal.Where(c => c.CreatedAt > since).ToListAsync(cancellationToken);

        if (relevant.Count == 0)
        {
            return new GetPropertyChangesResponse
            {
                // Monoton vorruecken, damit Since nicht irgendwann aus der Aufbewahrung faellt
                Watermark = Max(since, now - WatermarkSafetyMargin),
                FullResyncRequired = false,
                Changes = []
            };
        }

        // Pro Immobilie zur Netto-Aenderung deduplizieren. Deleted gewinnt immer:
        // der Client kann die Immobilie auch ausserhalb des Syncs geladen haben,
        // ein Tombstone fuer unbekannte Ids ist client-seitig ein No-Op.
        var netChanges = relevant
            .GroupBy(c => c.PropertyId)
            .Select(g =>
            {
                var ordered = g.OrderBy(c => c.CreatedAt).ToList();
                var changedAt = ordered[^1].CreatedAt;

                string changeType;
                if (ordered[^1].ChangeType == PropertyChangeTypes.Deleted)
                    changeType = PropertyChangeTypes.Deleted;
                else if (ordered.Any(c => c.ChangeType == PropertyChangeTypes.Created))
                    changeType = PropertyChangeTypes.Created;
                else
                    changeType = PropertyChangeTypes.Updated;

                return (PropertyId: g.Key, ChangeType: changeType, ChangedAt: changedAt);
            })
            .ToList();

        // Aktuelle Listendaten fuer Created/Updated in einem Rutsch nachladen
        var liveIds = netChanges
            .Where(c => c.ChangeType != PropertyChangeTypes.Deleted)
            .Select(c => c.PropertyId)
            .ToList();

        var baseUrl = GetPropertiesHandler.ResolveApiBaseUrl(httpContextAccessor, configuration);
        var properties = liveIds.Count == 0
            ? []
            : await dbContext.Set<Property>()
                .Include(p => p.Municipality)
                .Where(p => liveIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

        var changes = new List<PropertyChangeDto>(netChanges.Count);
        foreach (var (propertyId, changeType, changedAt) in netChanges)
        {
            if (changeType == PropertyChangeTypes.Deleted)
            {
                changes.Add(new PropertyChangeDto
                {
                    PropertyId = propertyId,
                    ChangeType = PropertyChangeTypes.Deleted,
                    ChangedAt = changedAt
                });
                continue;
            }

            // Inzwischen doch geloescht (Journal-Luecke o.ae.) -> defensiv als Deleted melden
            if (!properties.TryGetValue(propertyId, out var property))
            {
                changes.Add(new PropertyChangeDto
                {
                    PropertyId = propertyId,
                    ChangeType = PropertyChangeTypes.Deleted,
                    ChangedAt = changedAt
                });
                continue;
            }

            changes.Add(new PropertyChangeDto
            {
                PropertyId = propertyId,
                ChangeType = changeType,
                ChangedAt = changedAt,
                Property = new PropertyListItemDto(
                    property.Id,
                    property.Title,
                    property.Address,
                    property.MunicipalityId,
                    property.Municipality.Name,
                    property.Municipality.PostalCode,
                    property.Price,
                    property.LivingAreaSquareMeters,
                    property.PlotAreaSquareMeters,
                    property.Rooms,
                    property.Type,
                    property.SellerType,
                    property.SellerName,
                    GetPropertiesHandler.ProxyImageUrls(
                        property.ImageUrls,
                        baseUrl,
                        width: GetPropertiesHandler.ListThumbnailWidth),
                    property.CreatedAt,
                    property.InquiryType,
                    property.SourceName
                )
            });
        }

        return new GetPropertyChangesResponse
        {
            Watermark = relevant.Max(c => c.CreatedAt),
            FullResyncRequired = false,
            Changes = changes
        };
    }

    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;

    /// <summary>True wenn die Datenbank SQLite ist (lokale Entwicklung/Tests)</summary>
    public static bool IsSqlite(AppDbContext dbContext)
        => dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
}
