using Shiny.Mediator;

namespace Heimatplatz.Events;

/// <summary>
/// Event that is published when the page title changes
/// </summary>
public record PageTitleChangedEvent(string Title) : IEvent;
