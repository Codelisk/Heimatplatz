using Shiny.Mediator;

namespace Heimatplatz.Events;

/// <summary>
/// Event das nach erfolgreichem Login publiziert wird.
/// Wird vom LoginViewModel publiziert und von verschiedenen Features gehandled.
/// </summary>
public record UserLoggedInEvent(Guid UserId, string Email) : IEvent;
