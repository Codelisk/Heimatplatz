using Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Requests;

/// <summary>
/// Liefert alle persistierten Spans und Logs eines Traces (Waterfall-Daten).
/// </summary>
/// <param name="TraceId">W3C-Trace-Id (32 Hex-Zeichen)</param>
public record GetTraceDetailRequest(
    string TraceId
) : IRequest<GetTraceDetailResponse>;

/// <summary>
/// Spans (nach Startzeit) und Logs (nach Zeitstempel) des Traces.
/// </summary>
public record GetTraceDetailResponse(
    string TraceId,
    List<TraceSpanDto> Spans,
    List<TelemetryLogEntryDto> Logs
);
