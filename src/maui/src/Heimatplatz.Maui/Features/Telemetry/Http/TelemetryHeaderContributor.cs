using System.Diagnostics;
using Heimatplatz.Maui.Http;
using Shiny;

namespace Heimatplatz.Maui.Features.Telemetry.Http;

/// <summary>
/// Setzt auf jeden API-Call einen W3C-traceparent (frische Trace-Id pro Request,
/// Flags 01) und den X-Client-App-Header. Der Server uebernimmt die Trace-Id als
/// Remote-Parent - Server-Spans und -Logs desselben Requests tragen damit eine vom
/// Client stammende Trace-Id, und die Enrichment-Middleware haengt client.app an.
/// Prioritaet 100 = reserviertes Telemetrie-Band (siehe IHttpHeaderContributor).
/// </summary>
[Singleton]
public class TelemetryHeaderContributor : IHttpHeaderContributor
{
    public int Priority => 100;

    public Task ContributeAsync(HttpRequestMessage request, CancellationToken ct = default)
    {
        var traceparent = $"00-{ActivityTraceId.CreateRandom()}-{ActivitySpanId.CreateRandom()}-01";
        request.Headers.TryAddWithoutValidation("traceparent", traceparent);
        request.Headers.TryAddWithoutValidation(TelemetryClientInfo.ClientAppHeader, TelemetryClientInfo.ClientApp);
        return Task.CompletedTask;
    }
}
