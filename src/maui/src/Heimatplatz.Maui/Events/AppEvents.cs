using Shiny.Mediator;

namespace Heimatplatz.Maui.Events;

/// <summary>
/// Event zum Abmelden des Benutzers.
/// Wird vom UserProfile publiziert und von der Shell gehandled.
/// </summary>
public record LogoutRequestedEvent : IEvent;

/// <summary>
/// Event das nach erfolgreichem Login publiziert wird.
/// Wird vom LoginViewModel publiziert und von verschiedenen Features gehandled.
/// </summary>
public record UserLoggedInEvent(Guid UserId, string Email) : IEvent;
