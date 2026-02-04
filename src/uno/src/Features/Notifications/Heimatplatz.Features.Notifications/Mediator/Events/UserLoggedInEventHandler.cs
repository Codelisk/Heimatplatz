using Heimatplatz.Events;
using Heimatplatz.Features.Notifications.Contracts.Mediator.Commands;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Features.Notifications.Mediator.Events;

/// <summary>
/// Handler der nach erfolgreichem Login Push Notifications initialisiert.
/// Delegiert an InitializePushNotificationsCommand.
/// </summary>
public class UserLoggedInEventHandler(
    IMediator mediator,
    ILogger<UserLoggedInEventHandler> logger) : IEventHandler<UserLoggedInEvent>
{
    public async Task Handle(UserLoggedInEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation("[UserLoggedInEventHandler] User logged in: {Email}, initializing push notifications...", @event.Email);
        await mediator.Send(new InitializePushNotificationsCommand(), cancellationToken);
    }
}
