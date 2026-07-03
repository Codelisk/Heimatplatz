using Heimatplatz.Features.Notifications.Contracts.Mediator.Commands;
using Heimatplatz.Maui.Events;
using Heimatplatz.Maui.Features.Auth;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Services;

/// <summary>
/// App-Startlogik (ersetzt das ShellViewModel der Uno-App):
/// Session-Restore + Push-Initialisierung beim Start, Logout-Handling via Mediator-Event.
/// </summary>
[Singleton]
public class AppStartupService(
    IAuthService authService,
    IMediator mediator,
    INavigator navigator,
    ILogger<AppStartupService> logger
) : IEventHandler<LogoutRequestedEvent>
{
    /// <summary>
    /// Wird beim App-Start (nach Erstellung des Windows) aufgerufen.
    /// </summary>
    public async Task StartAsync()
    {
        var sessionRestored = await authService.TryRestoreSessionAsync();

        if (sessionRestored)
        {
            try
            {
                await mediator.Send(new InitializePushNotificationsCommand());
            }
            catch (Exception ex)
            {
                // Push nicht verfuegbar auf dieser Plattform - ignorieren
                logger.LogDebug(ex, "Push-Initialisierung beim Start uebersprungen");
            }
        }

#if ANDROID
        // In-App-Update-Check (fire-and-forget wie in der Uno-App)
        try
        {
            await mediator.Send(new Heimatplatz.Features.AppUpdate.Contracts.Mediator.Commands.CheckForAppUpdateCommand());
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "App-Update-Check fehlgeschlagen");
        }
#endif
    }

    /// <summary>
    /// Logout: Auth-State bereinigen und zur Login-Seite navigieren.
    /// </summary>
    public async Task Handle(LogoutRequestedEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        authService.ClearAuthentication();
        await navigator.NavigateTo("Login", relativeNavigation: false);
    }
}
