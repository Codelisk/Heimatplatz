using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Contracts.Mediator.Requests;

/// <summary>
/// Loescht eine Uebersicht samt aller Revisionen.
/// </summary>
public record DeleteDashboardRequest(Guid Id) : IRequest<DeleteDashboardResponse>;

/// <summary>
/// Response.
/// </summary>
public record DeleteDashboardResponse(
    bool Deleted
);
