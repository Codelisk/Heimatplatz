using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Contracts.Mediator.Requests;

/// <summary>
/// Liefert Status + validierte Definition einer Uebersicht. Zugleich der
/// Polling-Endpoint waehrend der KI-Generierung (Queued/InProgress -> Finished/Failed).
/// </summary>
public record GetDashboardRequest(Guid Id) : IRequest<GetDashboardResponse>;

/// <summary>
/// Response mit Status und (bei Finished) der Definition.
/// </summary>
/// <param name="Error">Nutzerfreundliche Fehlermeldung bei Status Failed</param>
/// <param name="Definition">Die validierte Definition; null solange keine Generierung abgeschlossen ist</param>
/// <param name="CanRevert">True wenn eine aeltere Fassung existiert, auf die zurueckgesprungen werden kann</param>
public record GetDashboardResponse(
    Guid Id,
    string Title,
    DashboardGenerationStatus Status,
    string? Error,
    DashboardDefinition? Definition,
    bool CanRevert,
    DateTimeOffset? GenerationRequestedAt,
    DateTimeOffset? GenerationCompletedAt
);
