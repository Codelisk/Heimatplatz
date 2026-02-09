using Heimatplatz.Core.Startup.Services;
using Heimatplatz.Events;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Auth.Presentation;
using Heimatplatz.Features.Notifications.Contracts.Mediator.Commands;
#if DEBUG
using Heimatplatz.Features.Debug.Presentation;
#endif
using Shiny.Mediator;
using Uno.Extensions.Navigation;

namespace Heimatplatz.App.Presentation;

public class ShellViewModel : IEventHandler<LogoutRequestedEvent>
{
    private readonly INavigator _navigator;
    private readonly IAuthService _authService;
    private readonly IMediator _mediator;
    private readonly IShinyReadinessService _shinyReadiness;

    public ShellViewModel(
        INavigator navigator,
        IAuthService authService,
        IMediator mediator,
        IShinyReadinessService shinyReadiness)
    {
        _navigator = navigator;
        _authService = authService;
        _mediator = mediator;
        _shinyReadiness = shinyReadiness;

        _ = Start();
    }

    public async Task Start()
    {
        // Versuche gespeicherte Session wiederherzustellen
        var sessionRestored = await _authService.TryRestoreSessionAsync();

        // Push Notifications initialisieren wenn Session wiederhergestellt wurde
        if (sessionRestored)
        {
            // Warten bis Shiny initialisiert ist (passiert in App.xaml.cs nach NavigateAsync)
            await _shinyReadiness.WaitForReadyAsync();

            try
            {
                await _mediator.Send(new InitializePushNotificationsCommand());
            }
            catch
            {
                // Push nicht verfuegbar auf dieser Plattform - ignorieren
            }
        }

#if DEBUG
        // Im Debug-Modus zur DebugStartPage navigieren für schnelles Testen
        await _navigator.NavigateViewModelAsync<DebugStartViewModel>(this);
#else
        // In Release zur MainPage navigieren
        await _navigator.NavigateViewModelAsync<MainViewModel>(this);
#endif
    }

    /// <summary>
    /// Handles logout request - clears auth and navigates to Login page.
    /// This handler runs at Shell level so it can navigate to Login (sibling of Main).
    /// </summary>
    public async Task Handle(LogoutRequestedEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        // Clear authentication state
        _authService.ClearAuthentication();

        // Navigate to Login from Shell level
        await _navigator.NavigateViewModelAsync<LoginViewModel>(this);
    }
}
