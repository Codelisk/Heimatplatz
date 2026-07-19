using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Heimatplatz.Api.Features.Telemetry.Infrastructure;

/// <summary>
/// OTel-Log-Prozessor: Warning+ geht sofort in die Writer-Queue (Error+ markiert
/// zusaetzlich den Trace fuer die Tail-Entscheidung), Info/Debug wird pro Trace
/// gepuffert und nur bei Fehler-Traces nachgereicht. Muss in OnEnd materialisieren,
/// da LogRecords gepoolt sind.
/// </summary>
public class TelemetryLogProcessor(
    TraceBufferService traceBuffer,
    TelemetryWriter writer,
    ErrorFingerprintService fingerprintService
) : BaseProcessor<LogRecord>
{
    private const int WarningRank = 3;
    private const int ErrorRank = 4;

    public override void OnEnd(LogRecord data)
    {
        try
        {
            // Eigene Kategorien nie verarbeiten (zweite Verteidigungslinie neben
            // SuppressInstrumentationScope im Writer)
            if (data.CategoryName?.StartsWith("Heimatplatz.Api.Features.Telemetry", StringComparison.Ordinal) == true)
                return;

            var (levelName, rank) = TelemetryMaterializer.MapLevel(data.LogLevel);
            var log = TelemetryMaterializer.Materialize(data, levelName);

            if (rank >= WarningRank)
            {
                ErrorFingerprint? fingerprint = null;
                if (data.Exception != null)
                {
                    var exceptionType = data.Exception.GetType().FullName ?? data.Exception.GetType().Name;
                    fingerprint = new ErrorFingerprint(
                        Hash: fingerprintService.Fingerprint(exceptionType, data.Exception.StackTrace, log.MessageTemplate),
                        ExceptionType: exceptionType,
                        Title: fingerprintService.BuildTitle(exceptionType, data.Exception.Message),
                        SampleMessage: data.Exception.Message,
                        SampleStackTrace: data.Exception.ToString());
                }

                if (rank >= ErrorRank && log.TraceId != null)
                    traceBuffer.MarkError(log.TraceId);

                writer.TryEnqueue(new LogWriteItem(log, fingerprint));
            }
            else if (log.TraceId != null)
            {
                traceBuffer.AddContextLog(log.TraceId, log);
            }
            // Info/Debug ohne Trace: bewusst verwerfen (kein Fehlerkontext moeglich)
        }
        catch
        {
            // Telemetrie darf nie den Log-Aufrufer brechen
        }
    }
}
