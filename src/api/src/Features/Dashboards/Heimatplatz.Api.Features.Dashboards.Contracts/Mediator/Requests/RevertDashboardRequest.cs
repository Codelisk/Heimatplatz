using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Contracts.Mediator.Requests;

/// <summary>
/// Spielt die vorherige Fassung der Uebersicht zurueck (kein KI-Aufruf).
/// Mehrfaches Zurueckspringen geht schrittweise weiter in die Historie.
/// Id im Body statt in der Route - OpenAPI-Generator-Kompatibilitaet.
/// </summary>
public class RevertDashboardRequest : IRequest<RevertDashboardResponse>
{
    public Guid Id { get; set; }
}

/// <summary>
/// Response; Reverted=false wenn keine aeltere Fassung existiert.
/// Der Client laedt danach GetDashboard neu.
/// </summary>
public record RevertDashboardResponse(
    bool Reverted
);
