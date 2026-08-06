using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Contracts.Mediator.Requests;

/// <summary>
/// Erstellt eine neue persoenliche Uebersicht aus dem Freitext-Wunsch des Nutzers.
/// Der Server legt das Dashboard mit Status Queued an und plant den KI-Hintergrund-Job
/// (TickerQ) ein; der Client pollt GetDashboard bis Finished/Failed.
/// Klasse mit Body-Properties statt Route-Parametern - Shiny-Mediator-OpenAPI-
/// Generator-Kompatibilitaet (gleiche Praezedenz wie GenerateDraftDescriptionRequest).
/// </summary>
public class GenerateDashboardRequest : IRequest<GenerateDashboardResponse>
{
    /// <summary>Der Wunsch in eigenen Worten: wonach gesucht wird und wie es angezeigt werden soll</summary>
    public string Prompt { get; set; } = "";
}

/// <summary>
/// Response mit der ID des neu angelegten Dashboards (Status Queued).
/// </summary>
public record GenerateDashboardResponse(
    Guid DashboardId,
    DashboardGenerationStatus Status
);
