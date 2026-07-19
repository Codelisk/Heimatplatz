using System.Threading.Channels;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Telemetry.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;

namespace Heimatplatz.Api.Features.Telemetry.Infrastructure;

/// <summary>
/// Zentraler Batch-Writer: nimmt Spans/Logs ueber eine bounded Channel-Queue entgegen
/// (DropWrite = fail-open bei Ueberlast) und schreibt sie gebatcht in die Datenbank.
/// Der komplette Flush laeuft unter SuppressInstrumentationScope, damit die eigenen
/// DB-Zugriffe keine neuen Spans/Logs erzeugen (Feedback-Loop). Raeumt ausserdem
/// periodisch verwaiste Trace-Puffer auf.
/// </summary>
public class TelemetryWriter(
    IServiceScopeFactory scopeFactory,
    TraceBufferService traceBuffer,
    ErrorGroupUpserter errorGroupUpserter,
    IOptions<TelemetryOptions> options,
    ILogger<TelemetryWriter> logger
) : BackgroundService
{
    private readonly Channel<TelemetryWriteItem> channel = Channel.CreateBounded<TelemetryWriteItem>(
        new BoundedChannelOptions(Math.Max(100, options.Value.WriterQueueCapacity))
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true
        });

    /// <summary>Nicht-blockierend; bei voller Queue wird das Element verworfen.</summary>
    public bool TryEnqueue(TelemetryWriteItem item) => channel.Writer.TryWrite(item);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var flushInterval = TimeSpan.FromSeconds(Math.Max(1, options.Value.WriterFlushIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Auf Daten warten, aber regelmaessig aufwachen, damit der Sweeper
                // auch bei Leerlauf verwaiste Trace-Puffer aufraeumt
                var waitForData = channel.Reader.WaitToReadAsync(stoppingToken).AsTask();
                await Task.WhenAny(waitForData, Task.Delay(flushInterval, stoppingToken));

                if (waitForData.IsCompletedSuccessfully && waitForData.Result)
                {
                    // Kurz sammeln, damit ein Batch zusammenkommt
                    await Task.Delay(flushInterval, stoppingToken);

                    var batch = new List<TelemetryWriteItem>();
                    while (batch.Count < options.Value.WriterMaxBatchSize && channel.Reader.TryRead(out var item))
                    {
                        batch.Add(item);
                    }

                    if (batch.Count > 0)
                        await FlushAsync(batch, stoppingToken);
                }

                SweepAbandonedBuckets();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "[Telemetry] Writer-Durchlauf fehlgeschlagen");
            }
        }

        // Best-effort: Restbestand beim Shutdown noch wegschreiben
        try
        {
            var remaining = new List<TelemetryWriteItem>();
            while (remaining.Count < options.Value.WriterMaxBatchSize && channel.Reader.TryRead(out var item))
            {
                remaining.Add(item);
            }

            if (remaining.Count > 0)
                await FlushAsync(remaining, CancellationToken.None);
        }
        catch
        {
            // Shutdown nie blockieren
        }
    }

    private async Task FlushAsync(List<TelemetryWriteItem> batch, CancellationToken ct)
    {
        // Verhindert, dass die eigenen DB-Zugriffe Spans (Npgsql-Instrumentierung)
        // oder Logs (OpenTelemetryLogger.IsEnabled prueft Sdk.SuppressInstrumentation)
        // in die Pipeline zurueckspeisen
        using var suppress = SuppressInstrumentationScope.Begin();

        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var fingerprinted = batch
                .OfType<LogWriteItem>()
                .Where(l => l.Fingerprint != null)
                .Select(l => (l.Log, l.Fingerprint!))
                .ToList();
            await errorGroupUpserter.ApplyAsync(dbContext, fingerprinted, ct);

            foreach (var item in batch)
            {
                switch (item)
                {
                    case SpanWriteItem span:
                        dbContext.Add(span.Span);
                        break;
                    case LogWriteItem log:
                        dbContext.Add(log.Log);
                        break;
                }
            }

            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Fail-open: Batch verwerfen, Landet nur auf der Console (OTel ist supprimiert,
            // Kategorie-Guard im LogProcessor greift zusaetzlich)
            logger.LogWarning(ex, "[Telemetry] Batch-Flush fehlgeschlagen, {Count} Eintraege verworfen", batch.Count);
        }
    }

    private void SweepAbandonedBuckets()
    {
        try
        {
            foreach (var bucket in traceBuffer.SweepAbandoned(DateTimeOffset.UtcNow))
            {
                foreach (var span in bucket.Spans)
                    TryEnqueue(new SpanWriteItem(span));
                foreach (var log in bucket.Logs)
                    TryEnqueue(new LogWriteItem(log, null));
            }
        }
        catch
        {
            // Sweep darf den Writer nie stoppen
        }
    }
}
