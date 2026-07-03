using Heimatplatz.Features.Notifications.Contracts.Mediator.Commands;
using Heimatplatz.Maui.Events;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Notifications.Handlers;

/// <summary>
/// Handler der nach erfolgreichem Login Push Notifications initialisiert.
/// Delegiert an InitializePushNotificationsCommand.
/// Kein Readiness-Gate noetig: Shiny ist via UseShiny() ab App-Start bereit.
/// </summary>
public class UserLoggedInEventHandler(
    IMediator mediator,
    ILogger<UserLoggedInEventHandler> logger) : IEventHandler<UserLoggedInEvent>
{
    public async Task Handle(UserLoggedInEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation("[UserLoggedInEventHandler] User logged in: {Email}, initializing push notifications...", @event.Email);
        try
        {
            await mediator.Send(new InitializePushNotificationsCommand(), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Push Notifications konnten nicht initialisiert werden (nicht auf dieser Plattform verfuegbar)");
        }
    }
}
