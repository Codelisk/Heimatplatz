using Heimatplatz.Events;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Auth.Presentation;
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

    public ShellViewModel(
        INavigator navigator,
        IAuthService authService)
    {
        _navigator = navigator;
        _authService = authService;

        _ = Start();
    }

    public async Task Start()
    {
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
