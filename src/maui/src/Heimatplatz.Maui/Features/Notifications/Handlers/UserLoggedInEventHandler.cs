using Heimatplatz.Features.Notifications.Contracts.Mediator.Commands;
using Heimatplatz.Maui.Events;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Notifications.Handlers;

/// <summary>
/// Handler der nach erfolgreichem Login Push Notifications initialisiert.
/// Delegiert im Hintergrund an InitializePushNotificationsCommand, damit APNs/FCM
/// die Navigation nach erfolgreichem Login nicht blockieren.
/// Kein Readiness-Gate noetig: Shiny ist via UseShiny() ab App-Start bereit.
/// </summary>
public class UserLoggedInEventHandler(
    IMediator mediator,
    ILogger<UserLoggedInEventHandler> logger) : IEventHandler<UserLoggedInEvent>
{
    public Task Handle(UserLoggedInEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation("[UserLoggedInEventHandler] User logged in: {Email}, initializing push notifications...", @event.Email);

        // Die Push-Registrierung kann auf iOS mehrere Sekunden auf den APNs-Callback
        // warten. Sie ist kein Bestandteil des Logins und darf dessen Navigation nicht
        // blockieren. Exceptions werden innerhalb der Hintergrundaufgabe beobachtet.
        _ = InitializeSafeAsync();
        return Task.CompletedTask;
    }

    private async Task InitializeSafeAsync()
    {
        try
        {
            await mediator.Send(new InitializePushNotificationsCommand(), CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Push Notifications konnten nicht initialisiert werden (nicht auf dieser Plattform verfuegbar)");
        }
    }
}
