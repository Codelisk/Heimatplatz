using Shiny.Mediator;

namespace Heimatplatz.Events;

/// <summary>
/// Event zum Zurücknavigieren in der Content-Region der NavigationView.
/// Wird vom AppHeaderLeft (Back-Button) publiziert und von MainPage gehandled.
/// </summary>
public record NavigateBackInContentEvent : IEvent;
