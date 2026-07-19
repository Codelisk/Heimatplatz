using System.Collections.Concurrent;
using Heimatplatz.Api.Features.Telemetry.Configuration;
using Heimatplatz.Api.Features.Telemetry.Data.Entities;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.Telemetry.Infrastructure;

/// <summary>
/// Puffert Spans und Kontext-Logs (Info/Debug) pro Trace bis zur Tail-Sampling-
/// Entscheidung beim Ende des lokalen Root-Spans. Fehler-Logs markieren den Trace,
/// damit er samt Kontext persistiert wird. Alle Caps sind Schutz vor Speicherwachstum
/// (fail-open: bei vollen Puffern wird verworfen, nie blockiert).
/// </summary>
public class TraceBufferService(IOptions<TelemetryOptions> options)
{
    private readonly ConcurrentDictionary<string, TraceBucket> buckets = new();

    public sealed class TraceBucket
    {
        public Lock Lock { get; } = new();
        public List<TelemetrySpan> Spans { get; } = [];
        public List<TelemetryLog> Logs { get; } = [];
        public bool HasError { get; set; }
        public DateTimeOffset CreatedUtc { get; init; }
    }

    public void AddSpan(string traceId, TelemetrySpan span)
    {
        var bucket = GetOrCreateBucket(traceId);
        if (bucket == null)
            return;

        lock (bucket.Lock)
        {
            if (bucket.Spans.Count < options.Value.MaxSpansPerTrace)
                bucket.Spans.Add(span);
        }
    }

    public void AddContextLog(string traceId, TelemetryLog log)
    {
        var bucket = GetOrCreateBucket(traceId);
        if (bucket == null)
            return;

        lock (bucket.Lock)
        {
            if (bucket.Logs.Count < options.Value.MaxLogsPerTrace)
                bucket.Logs.Add(log);
        }
    }

    /// <summary>
    /// Markiert den Trace als fehlerhaft (Error-Log aufgetreten) - er wird beim
    /// Root-Ende bzw. vom Sweeper komplett persistiert.
    /// </summary>
    public void MarkError(string traceId)
    {
        var bucket = GetOrCreateBucket(traceId);
        if (bucket == null)
            return;

        lock (bucket.Lock)
        {
            bucket.HasError = true;
        }
    }

    /// <summary>
    /// Entnimmt den Puffer des Traces fuer die Tail-Entscheidung (null wenn keiner existiert).
    /// </summary>
    public TraceBucket? TryRemove(string traceId)
        => buckets.TryRemove(traceId, out var bucket) ? bucket : null;

    /// <summary>
    /// Raeumt verwaiste Puffer auf (Root-Span nie beendet, z.B. abgebrochene Requests):
    /// fehlermarkierte Puffer werden zum Persistieren zurueckgegeben, gesunde verworfen.
    /// </summary>
    public List<TraceBucket> SweepAbandoned(DateTimeOffset now)
    {
        var timeout = TimeSpan.FromMinutes(options.Value.AbandonedTraceTimeoutMinutes);
        List<TraceBucket>? toPersist = null;

        foreach (var (traceId, bucket) in buckets)
        {
            if (now - bucket.CreatedUtc < timeout)
                continue;

            if (buckets.TryRemove(traceId, out var removed) && removed.HasError)
            {
                toPersist ??= [];
                toPersist.Add(removed);
            }
        }

        return toPersist ?? [];
    }

    private TraceBucket? GetOrCreateBucket(string traceId)
    {
        if (buckets.TryGetValue(traceId, out var existing))
            return existing;

        // Cap auf die Gesamtzahl gepufferter Traces: neue Traces werden bei vollem
        // Puffer nicht mehr aufgezeichnet (fail-open)
        if (buckets.Count >= options.Value.MaxBufferedTraces)
            return null;

        return buckets.GetOrAdd(traceId, _ => new TraceBucket { CreatedUtc = DateTimeOffset.UtcNow });
    }
}
