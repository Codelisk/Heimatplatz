using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Contracts.Mediator.Requests;

/// <summary>
/// Daten-Ebene: loest die Queries aller Widgets der Ansicht serverseitig auf
/// (in-process ueber die bestehenden Properties-Requests - Blockiert-Ausschluss,
/// IsHidden-Moderation und Bild-Regeln greifen automatisch) und liefert
/// anzeigefertige Payloads. Keine KI beteiligt; laeuft bei jedem Aufruf.
/// <paramref name="Page"/> blaettert die Trefferliste (Listen-Ansicht,
/// Startseiten-Paging-Muster); alle anderen Widgets ignorieren sie.
/// </summary>
public record GetDashboardDataRequest(Guid Id, int Page = 0) : IRequest<GetDashboardDataResponse>;

/// <summary>
/// Response mit den Daten aller Widgets (fail-soft je Widget).
/// </summary>
public record GetDashboardDataResponse(
    List<WidgetDataDto> Widgets
);
