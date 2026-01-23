using Shiny.Mediator;

namespace Heimatplatz.Events;

/// <summary>
/// Event zum Umschalten des NavigationView Pane (Hamburger Menu).
/// Wird vom AppHeader publiziert und von MainPage gehandled.
/// </summary>
public record ToggleNavigationPaneEvent : IEvent;
