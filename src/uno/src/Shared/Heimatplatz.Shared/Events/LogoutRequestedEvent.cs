using Shiny.Mediator;

namespace Heimatplatz.Events;

/// <summary>
/// Event zum Abmelden des Benutzers.
/// Wird vom AppHeaderRight publiziert und von Shell gehandled.
/// </summary>
public record LogoutRequestedEvent : IEvent;
