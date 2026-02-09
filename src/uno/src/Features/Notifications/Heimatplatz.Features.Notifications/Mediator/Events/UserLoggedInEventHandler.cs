using Heimatplatz.Core.Startup.Services;
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
    IShinyReadinessService shinyReadiness,
    ILogger<UserLoggedInEventHandler> logger) : IEventHandler<UserLoggedInEvent>
{
    public async Task Handle(UserLoggedInEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation("[UserLoggedInEventHandler] User logged in: {Email}, initializing push notifications...", @event.Email);
        try
        {
            // Warten bis Shiny initialisiert ist (auf Mobile)
            await shinyReadiness.WaitForReadyAsync();

            await mediator.Send(new InitializePushNotificationsCommand(), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Push Notifications konnten nicht initialisiert werden (nicht auf dieser Plattform verfuegbar)");
        }
    }
}
