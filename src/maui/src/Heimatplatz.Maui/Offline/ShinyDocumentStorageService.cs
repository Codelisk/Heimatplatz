using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.DocumentDb;
using Shiny.Mediator.Infrastructure;

namespace Heimatplatz.Maui.Offline;

/// <summary>
/// SQLite-Implementierung des von Shiny Mediator verwendeten Key/Value-Stores.
/// Damit liegen Offline-Antworten und persistenter Cache in Shiny DocumentDb
/// statt in einzelnen Dateien im vom Betriebssystem loeschbaren Cache-Ordner.
/// </summary>
internal sealed class ShinyDocumentStorageService(
    IDocumentStore store,
    ISerializer serializer,
    ILogger<ShinyDocumentStorageService> logger
) : IStorageService
{
    public async Task Set<T>(
        string category,
        string key,
        T value,
        CancellationToken cancellationToken)
    {
        var record = new MediatorStorageRecord(
            GetId(category, key),
            category,
            key,
            serializer.Serialize(value),
            DateTimeOffset.UtcNow);

        await store.Upsert(
                record,
                patchIfUpdate: false,
                OfflineStorageJsonContext.Default.MediatorStorageRecord,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<T?> Get<T>(
        string category,
        string key,
        CancellationToken cancellationToken)
    {
        var id = GetId(category, key);
        var record = await store.Get(
                id,
                OfflineStorageJsonContext.Default.MediatorStorageRecord,
                cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
            return default;

        try
        {
            return serializer.Deserialize<T>(record.Json);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Entferne defekten lokalen Eintrag {Category}/{Key}", category, key);
            await store.Remove<MediatorStorageRecord>(id, cancellationToken).ConfigureAwait(false);
            return default;
        }
    }

    public async Task Remove(
        string category,
        string requestKey,
        bool partialMatchKey = false,
        CancellationToken cancellationToken = default)
    {
        if (!partialMatchKey)
        {
            await store.Remove<MediatorStorageRecord>(GetId(category, requestKey), cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        var records = await store
            .Query(OfflineStorageJsonContext.Default.MediatorStorageRecord)
            .Where(x => x.Category == category && x.Key.StartsWith(requestKey))
            .ToList(cancellationToken)
            .ConfigureAwait(false);

        foreach (var record in records)
        {
            await store.Remove<MediatorStorageRecord>(record.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task Clear(string category, CancellationToken cancellationToken)
    {
        var records = await store
            .Query(OfflineStorageJsonContext.Default.MediatorStorageRecord)
            .Where(x => x.Category == category)
            .ToList(cancellationToken)
            .ConfigureAwait(false);

        foreach (var record in records)
        {
            await store.Remove<MediatorStorageRecord>(record.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string GetId(string category, string key) => $"{category}\u001f{key}";
}

internal sealed record MediatorStorageRecord(
    string Id,
    string Category,
    string Key,
    string Json,
    DateTimeOffset UpdatedAt);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(MediatorStorageRecord))]
internal sealed partial class OfflineStorageJsonContext : JsonSerializerContext;
