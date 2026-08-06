using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Contracts.Mediator.Requests;

/// <summary>
/// Startet eine Verfeinerungsrunde: die KI ueberarbeitet die bestehende Definition
/// nach der Anweisung des Nutzers ("Mach die Karte groesser", "nur noch Privatanbieter").
/// Die bisherige Fassung bleibt als Revision erhalten (RevertDashboard).
/// Id im Body statt in der Route - OpenAPI-Generator-Kompatibilitaet.
/// </summary>
public class RefineDashboardRequest : IRequest<RefineDashboardResponse>
{
    public Guid Id { get; set; }

    /// <summary>Die Aenderungs-Anweisung in eigenen Worten</summary>
    public string Instruction { get; set; } = "";
}

/// <summary>
/// Response mit dem neuen Generierungs-Status (Queued).
/// </summary>
public record RefineDashboardResponse(
    DashboardGenerationStatus Status
);
