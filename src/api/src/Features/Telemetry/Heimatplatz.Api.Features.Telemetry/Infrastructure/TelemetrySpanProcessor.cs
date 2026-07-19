using System.Diagnostics;
using Heimatplatz.Api.Features.Telemetry.Configuration;
using Microsoft.Extensions.Options;
using OpenTelemetry;

namespace Heimatplatz.Api.Features.Telemetry.Infrastructure;

/// <summary>
/// OTel-Span-Prozessor mit In-Process-Tail-Sampling: Child-Spans werden pro Trace
/// gepuffert; beim Ende des lokalen Root-Spans (Activity.Parent == null - deckt auch
/// Remote-Parents via traceparent ab) faellt die Entscheidung, ob der komplette Trace
/// (Spans + gepufferte Kontext-Logs) persistiert oder verworfen wird.
/// </summary>
public class TelemetrySpanProcessor(
    TraceBufferService traceBuffer,
    TelemetryWriter writer,
    IOptions<TelemetryOptions> options
) : BaseProcessor<Activity>
{
    public override void OnEnd(Activity data)
    {
        try
        {
            var span = TelemetryMaterializer.Materialize(data);

            if (data.Parent != null)
            {
                traceBuffer.AddSpan(span.TraceId, span);
                return;
            }

            // Lokaler Root -> Tail-Entscheidung
            var bucket = traceBuffer.TryRemove(span.TraceId);
            var keep = data.Status == ActivityStatusCode.Error
                || bucket is { HasError: true }
                || data.Duration.TotalMilliseconds > options.Value.SlowRequestThresholdMs
                || Random.Shared.NextDouble() * 100 < options.Value.SampleHealthyTracePercent;

            if (!keep)
                return;

            writer.TryEnqueue(new SpanWriteItem(span));
            if (bucket == null)
                return;

            foreach (var buffered in bucket.Spans)
                writer.TryEnqueue(new SpanWriteItem(buffered));
            foreach (var log in bucket.Logs)
                writer.TryEnqueue(new LogWriteItem(log, null));
        }
        catch
        {
            // Telemetrie darf nie den Request brechen
        }
    }
}
