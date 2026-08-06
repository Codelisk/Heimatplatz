using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Contracts.Mediator.Requests;

/// <summary>
/// Benennt eine Uebersicht um (der KI-Titel ist nur ein Vorschlag).
/// Id im Body statt in der Route - OpenAPI-Generator-Kompatibilitaet.
/// </summary>
public class UpdateDashboardRequest : IRequest<UpdateDashboardResponse>
{
    public Guid Id { get; set; }

    public string Title { get; set; } = "";
}

/// <summary>
/// Response.
/// </summary>
public record UpdateDashboardResponse(
    bool Updated
);
