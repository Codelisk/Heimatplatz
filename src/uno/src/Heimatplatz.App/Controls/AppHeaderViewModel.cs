using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Events;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Shiny.Mediator;
using Uno.Extensions.Navigation;
using UnoFramework.Contracts.Pages;

namespace Heimatplatz.App.Controls;

/// <summary>
/// ViewModel fuer den AppHeader.
/// Verwaltet HeaderMode (Menu/Normal), Page-Titel, Auth-Status und Navigation.
/// Empfaengt PageHeaderChangedEvent und HeaderModeChangedEvent via Shiny Mediator.
/// </summary>
public partial class AppHeaderViewModel : ObservableObject,
    IEventHandler<PageHeaderChangedEvent>,
    IEventHandler<HeaderModeChangedEvent>
{
    private readonly IAuthService _authService;
    private readonly IMediator _mediator;
    private readonly INavigator _navigator;
    private readonly ILogger<AppHeaderViewModel> _logger;

    /// <summary>
    /// Current header mode (Menu/Normal)
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMenuMode))]
    [NotifyPropertyChangedFor(nameof(IsNormalMode))]
    private HeaderMode _headerMode = HeaderMode.Menu;

    /// <summary>
    /// Visibility for hamburger button (Menu mode)
    /// </summary>
    public Visibility IsMenuMode =>
        HeaderMode == HeaderMode.Menu ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Visibility for back button (Normal mode)
    /// </summary>
    public Visibility IsNormalMode =>
        HeaderMode == HeaderMode.Normal ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Aktueller Page-Titel - wird direkt vom Event gesetzt
    /// </summary>
    [ObservableProperty]
    private string _currentTitle = "HEIMATPLATZ";

    /// <summary>
    /// Header-Content for center area
    /// </summary>
    [ObservableProperty]
    private object? _headerContent;

    /// <summary>
    /// Auth state - visible when authenticated
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotAuthenticated))]
    private Visibility _isAuthenticated = Visibility.Collapsed;

    /// <summary>
    /// Inverse of IsAuthenticated for XAML binding
    /// </summary>
    public Visibility IsNotAuthenticated =>
        IsAuthenticated == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

    [ObservableProperty]
    private string? _userFullName;

    [ObservableProperty]
    private string? _userInitials;

    public AppHeaderViewModel(
        IAuthService authService,
        IMediator mediator,
        INavigator navigator,
        ILogger<AppHeaderViewModel> logger)
    {
        _authService = authService;
        _mediator = mediator;
        _navigator = navigator;
        _logger = logger;

        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
        UpdateAuthState();

        _logger.LogInformation("[AppHeader] Initialisiert - HeaderMode: {Mode}, CurrentTitle: {Title}, IsAuthenticated: {IsAuth}",
            HeaderMode, CurrentTitle, IsAuthenticated);
    }

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        _logger.LogInformation("[AppHeader] Auth State Changed: {IsAuth}", isAuthenticated);
        UpdateAuthState();
    }

    private void UpdateAuthState()
    {
        IsAuthenticated = _authService.IsAuthenticated ? Visibility.Visible : Visibility.Collapsed;
        UserFullName = _authService.UserFullName;
        UserInitials = GetInitials(_authService.UserFullName);

        _logger.LogDebug("[AppHeader] UpdateAuthState - IsAuthenticated: {IsAuth}, UserFullName: {Name}",
            IsAuthenticated, UserFullName);
    }

    private static string? GetInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return null;

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return null;

        if (parts.Length == 1)
            return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    /// <summary>
    /// Logout command
    /// </summary>
    [RelayCommand]
    private void Logout()
    {
        _logger.LogInformation("[AppHeader] Logout initiated");
        _authService.ClearAuthentication();
    }

    /// <summary>
    /// Toggles the NavigationView pane (Hamburger menu) via Mediator event
    /// </summary>
    [RelayCommand]
    private async Task ToggleNavigationPane()
    {
        _logger.LogDebug("[AppHeader] ToggleNavigationPane");
        await _mediator.Publish(new ToggleNavigationPaneEvent());
    }

    /// <summary>
    /// Navigates back in the navigation stack
    /// </summary>
    [RelayCommand]
    private async Task GoBack()
    {
        _logger.LogDebug("[AppHeader] GoBack");
        await _navigator.NavigateBackAsync(this);
    }

    /// <summary>
    /// Handles the PageHeaderChangedEvent - updates title and header content
    /// </summary>
    public Task Handle(PageHeaderChangedEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        var newTitle = string.IsNullOrWhiteSpace(@event.Title) ? "HEIMATPLATZ" : @event.Title;
        _logger.LogInformation("[AppHeader] PageHeaderChangedEvent - Title: {Title}, HasContent: {HasContent}",
            newTitle, @event.HeaderContent != null);

        // Ensure property changes happen on UI thread
        var dispatcherQueue = App.MainWindow?.DispatcherQueue;
        if (dispatcherQueue != null)
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                CurrentTitle = newTitle;
                HeaderContent = @event.HeaderContent;
                _logger.LogDebug("[AppHeader] Updated - Title: {Title}, HeaderContent: {HasContent}",
                    CurrentTitle, HeaderContent != null);
            });
        }
        else
        {
            _logger.LogWarning("[AppHeader] DispatcherQueue is null, updating directly");
            CurrentTitle = newTitle;
            HeaderContent = @event.HeaderContent;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the HeaderModeChangedEvent - updates header mode (Menu/Normal)
    /// </summary>
    public Task Handle(HeaderModeChangedEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[AppHeader] HeaderModeChangedEvent - Mode: {Mode}", @event.Mode);

        // Ensure property changes happen on UI thread
        var dispatcherQueue = App.MainWindow?.DispatcherQueue;
        if (dispatcherQueue != null)
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                HeaderMode = @event.Mode;
                _logger.LogDebug("[AppHeader] HeaderMode updated to: {Mode}", HeaderMode);
            });
        }
        else
        {
            _logger.LogWarning("[AppHeader] DispatcherQueue is null, updating HeaderMode directly");
            HeaderMode = @event.Mode;
        }

        return Task.CompletedTask;
    }
}
