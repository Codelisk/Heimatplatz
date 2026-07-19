using System.Collections.Concurrent;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Telemetry.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Maui.Features.Telemetry.Logging;

/// <summary>
/// ILogger-Provider (Release): sammelt Warning+ in einer bounded Queue und sendet
/// sie alle 30 Sekunden gebatcht an die API. Eigene Telemetrie-Kategorien werden
/// uebersprungen (Loop-Schutz), bei vollem Puffer oder Sendefehlern wird verworfen
/// (fail-open). TelemetryLogSender wird lazy aufgeloest - beim Bau des LoggerFactory
/// existiert der Mediator noch nicht.
/// </summary>
public sealed class RemoteTelemetryLoggerProvider : ILoggerProvider
{
    private const int MaxQueuedEntries = 100;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceProvider serviceProvider;
    private readonly ConcurrentQueue<ClientLogEntryDto> queue = new();
    private readonly Timer flushTimer;
    private int queuedCount;
    private int flushing;

    public RemoteTelemetryLoggerProvider(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        flushTimer = new Timer(_ => _ = FlushAsync(), null, FlushInterval, FlushInterval);
    }

    public ILogger CreateLogger(string categoryName)
        => categoryName.StartsWith("Heimatplatz.Maui.Features.Telemetry", StringComparison.Ordinal)
            ? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance
            : new RemoteLogger(this, categoryName);

    public void Dispose()
    {
        flushTimer.Dispose();
        _ = FlushAsync();
    }

    private void Enqueue(ClientLogEntryDto entry)
    {
        if (Interlocked.Increment(ref queuedCount) > MaxQueuedEntries)
        {
            Interlocked.Decrement(ref queuedCount);
            return;
        }

        queue.Enqueue(entry);
    }

    private async Task FlushAsync()
    {
        // Nur ein Flush gleichzeitig
        if (Interlocked.Exchange(ref flushing, 1) == 1)
            return;

        try
        {
            while (!queue.IsEmpty)
            {
                var batch = new List<ClientLogEntryDto>(TelemetryLogSender.MaxBatchSize);
                while (batch.Count < TelemetryLogSender.MaxBatchSize && queue.TryDequeue(out var entry))
                {
                    Interlocked.Decrement(ref queuedCount);
                    batch.Add(entry);
                }

                if (batch.Count == 0)
                    return;

                var sender = serviceProvider.GetRequiredService<TelemetryLogSender>();
                if (!await sender.TrySendAsync(batch))
                {
                    // Offline/Fehler: Batch verwerfen statt requeuen (fail-open,
                    // Crash-Reports gehen separat ueber den Preferences-Puffer)
                    return;
                }
            }
        }
        catch
        {
            // Telemetrie darf die App nie beeintraechtigen
        }
        finally
        {
            Interlocked.Exchange(ref flushing, 0);
        }
    }

    private sealed class RemoteLogger(RemoteTelemetryLoggerProvider provider, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            try
            {
                provider.Enqueue(new ClientLogEntryDto
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Level = logLevel.ToString(),
                    Message = $"{categoryName}: {formatter(state, exception)}",
                    ExceptionText = exception?.ToString()
                });
            }
            catch
            {
                // fail-open
            }
        }
    }
}
