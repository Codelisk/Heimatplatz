using Shiny.Mediator;

namespace Heimatplatz.Events;

/// <summary>
/// Event zum Navigieren zu einer Route in der Content-Region der NavigationView.
/// Wird von Controls ausserhalb der Content-Region (z.B. AppHeaderRight) publiziert
/// und von MainPage gehandled, um die Navigation im richtigen Kontext auszufuehren.
/// </summary>
public record NavigateToRouteInContentEvent(string Route) : IEvent;
