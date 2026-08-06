using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Contracts.Mediator.Requests;

/// <summary>
/// Listet alle Uebersichten des angemeldeten Nutzers (neueste zuerst).
/// </summary>
public record GetDashboardsRequest() : IRequest<GetDashboardsResponse>;

/// <summary>
/// Response mit den Uebersichten des Nutzers.
/// </summary>
public record GetDashboardsResponse(
    List<DashboardSummaryDto> Dashboards
);

/// <summary>
/// Listenzeile einer Uebersicht (ohne Definition - die liefert GetDashboard).
/// </summary>
public record DashboardSummaryDto(
    Guid Id,
    string Title,
    DashboardGenerationStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
