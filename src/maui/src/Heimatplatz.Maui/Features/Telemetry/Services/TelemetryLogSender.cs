using Heimatplatz.Maui.ApiClient.Generated;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Telemetry.Services;

/// <summary>
/// Sendet Client-Logs/Crash-Reports als Batch an POST /api/telemetry/client-logs
/// (anonym, serverseitig rate-limitiert und gecappt). Fail-open: Fehlschlaege werden
/// nur auf Debug-Level geloggt (Warning wuerde vom RemoteTelemetryLoggerProvider
/// erneut eingesammelt - Endlosschleife).
/// </summary>
[Singleton]
public class TelemetryLogSender(
    IMediator mediator,
    ILogger<TelemetryLogSender> logger
)
{
    /// <summary>Server-Cap MaxBatchEntries - groessere Batches ergeben 400</summary>
    public const int MaxBatchSize = 20;

    public async Task<bool> TrySendAsync(IReadOnlyCollection<ClientLogEntryDto> entries, CancellationToken ct = default)
    {
        if (entries.Count == 0)
            return true;

        try
        {
            await mediator.Request(
                new IngestClientLogsHttpRequest
                {
                    Body = new IngestClientLogsRequest
                    {
                        Source = TelemetrySource.Maui,
                        AppVersion = TelemetryClientInfo.AppVersion,
                        Platform = TelemetryClientInfo.Platform,
                        Entries = entries.Take(MaxBatchSize).ToList()
                    }
                },
                ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Telemetrie-Upload fehlgeschlagen ({Count} Eintraege)", entries.Count);
            return false;
        }
    }

    /// <summary>
    /// Sendet beim App-Start die persistierten Crash-Reports des letzten Laufs;
    /// bei Erfolg wird der Preferences-Puffer geleert, sonst bleibt er fuer den
    /// naechsten Start erhalten.
    /// </summary>
    public async Task SendPendingCrashReportsAsync(CancellationToken ct = default)
    {
        var pending = CrashReportStore.Read();
        if (pending.Count == 0)
            return;

        var entries = pending
            .Select(p => new ClientLogEntryDto
            {
                TimestampUtc = p.TimestampUtc,
                Level = p.Level,
                Message = p.Message,
                ExceptionText = p.ExceptionText,
                Screen = p.Screen
            })
            .ToList();

        if (await TrySendAsync(entries, ct))
        {
            CrashReportStore.Clear();
        }
    }
}
