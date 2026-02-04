using Heimatplatz.Features.Notifications.Contracts.Interfaces;
using Heimatplatz.Features.Notifications.Contracts.Mediator.Commands;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Features.Notifications.Mediator.Commands;

/// <summary>
/// Handler fuer die Push Notification Initialisierung.
/// Delegiert an IPushNotificationInitializer, der plattformspezifische
/// Implementierungen bereitstellt (Shiny.Push auf Mobile, No-Op auf Desktop/WASM).
/// </summary>
[MediatorSingleton]
public class InitializePushNotificationsCommandHandler(
    IPushNotificationInitializer pushNotificationInitializer,
    ILogger<InitializePushNotificationsCommandHandler> logger) : ICommandHandler<InitializePushNotificationsCommand>
{
    public async Task Handle(InitializePushNotificationsCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation("[InitializePushNotifications] Initialisiere Push Notifications...");
        await pushNotificationInitializer.InitializeAsync();
    }
}
