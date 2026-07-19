using Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Requests;

/// <summary>
/// Fehler-Statistik: Fehler/Warnungen pro Tag und Top-Fehlergruppen im Zeitraum.
/// </summary>
/// <param name="Days">Betrachtungszeitraum in Tagen (rueckwirkend ab heute, UTC)</param>
/// <param name="TopGroupCount">Anzahl der Top-Fehlergruppen im Ergebnis</param>
public record GetTelemetryStatsRequest(
    int Days = 7,
    int TopGroupCount = 10
) : IRequest<GetTelemetryStatsResponse>;

/// <summary>
/// Tageszaehler (aufsteigend nach Datum) und Top-Gruppen (nach Haeufigkeit im Zeitraum).
/// </summary>
public record GetTelemetryStatsResponse(
    List<DailyErrorCountDto> Daily,
    List<ErrorGroupSummaryDto> TopGroups,
    int TotalErrors,
    int TotalWarnings
);
