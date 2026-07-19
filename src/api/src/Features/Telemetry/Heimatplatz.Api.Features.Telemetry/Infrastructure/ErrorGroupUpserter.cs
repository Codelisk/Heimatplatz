using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Telemetry.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Telemetry.Infrastructure;

/// <summary>
/// Upsert von Fehlergruppen anhand des Fingerprint-Hashes und Zuordnung der
/// Log-Eintraege. Wird vom TelemetryWriter (Batch) und vom Ingestion-Handler
/// genutzt; speichert selbst nicht - SaveChanges macht der Aufrufer.
/// </summary>
public class ErrorGroupUpserter
{
    public async Task ApplyAsync(
        AppDbContext dbContext,
        IReadOnlyCollection<(TelemetryLog Log, ErrorFingerprint Fingerprint)> entries,
        CancellationToken ct)
    {
        if (entries.Count == 0)
            return;

        var hashes = entries.Select(e => e.Fingerprint.Hash).Distinct().ToList();

        // Auch lokal getrackte (noch nicht gespeicherte) Gruppen beruecksichtigen,
        // damit mehrere Batches im selben Kontext keine Duplikate anlegen
        var groups = await dbContext.Set<TelemetryErrorGroup>()
            .Where(g => hashes.Contains(g.FingerprintHash))
            .ToDictionaryAsync(g => g.FingerprintHash, ct);
        foreach (var tracked in dbContext.ChangeTracker.Entries<TelemetryErrorGroup>())
        {
            groups.TryAdd(tracked.Entity.FingerprintHash, tracked.Entity);
        }

        foreach (var (log, fingerprint) in entries)
        {
            if (!groups.TryGetValue(fingerprint.Hash, out var group))
            {
                group = new TelemetryErrorGroup
                {
                    Id = Guid.CreateVersion7(),
                    FingerprintHash = fingerprint.Hash,
                    ExceptionType = fingerprint.ExceptionType,
                    Title = fingerprint.Title,
                    SampleMessage = fingerprint.SampleMessage,
                    SampleStackTrace = fingerprint.SampleStackTrace,
                    FirstSeenUtc = log.TimestampUtc,
                    LastSeenUtc = log.TimestampUtc,
                    OccurrenceCount = 0
                };
                dbContext.Add(group);
                groups[fingerprint.Hash] = group;
            }

            group.OccurrenceCount++;
            if (log.TimestampUtc > group.LastSeenUtc)
                group.LastSeenUtc = log.TimestampUtc;
            if (log.TraceId != null)
                group.LastTraceId = log.TraceId;

            log.ErrorGroupId = group.Id;
        }
    }
}
