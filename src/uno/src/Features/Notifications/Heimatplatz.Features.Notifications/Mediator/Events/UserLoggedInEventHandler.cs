using Heimatplatz.Events;
using Heimatplatz.Features.Notifications.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Features.Notifications.Mediator.Events;

/// <summary>
/// Handler der nach erfolgreichem Login Push Notifications initialisiert.
/// </summary>
public class UserLoggedInEventHandler(
    IPushNotificationInitializer pushNotificationInitializer,
    ILogger<UserLoggedInEventHandler> logger) : IEventHandler<UserLoggedInEvent>
{
    public async Task Handle(UserLoggedInEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation("[UserLoggedInEventHandler] User logged in: {Email}, initializing push notifications...", @event.Email);

        await pushNotificationInitializer.InitializeAsync();
    }
}
