using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Events;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Shiny.Mediator;
using Uno.Extensions.Navigation;

namespace Heimatplatz.App.Controls;

/// <summary>
/// ViewModel fuer den AppHeader
/// Verwaltet den Authentifizierungsstatus, Logout-Funktionalitaet und Page-Titel
/// Empfaengt PageHeaderChangedEvent via Shiny Mediator
/// </summary>
public partial class AppHeaderViewModel : ObservableObject, IEventHandler<PageHeaderChangedEvent>
{
    private readonly IAuthService _authService;
    private readonly IMediator _mediator;
    private readonly ILogger<AppHeaderViewModel> _logger;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotAuthenticated))]
    private Visibility _isAuthenticated = Visibility.Collapsed;

    /// <summary>
    /// Inverse von IsAuthenticated fuer XAML-Binding
    /// </summary>
    public Visibility IsNotAuthenticated =>
        IsAuthenticated == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

    [ObservableProperty]
    private string? _userFullName;

    [ObservableProperty]
    private string? _userInitials;

    /// <summary>
    /// Aktueller Page-Titel - wird direkt vom Event gesetzt
    /// </summary>
    [ObservableProperty]
    private string _currentTitle = "HEIMATPLATZ";

    /// <summary>
    /// Header-Content
    /// </summary>
    [ObservableProperty]
    private object? _headerContent;

    public AppHeaderViewModel(
        IAuthService authService,
        IMediator mediator,
        ILogger<AppHeaderViewModel> logger)
    {
        _authService = authService;
        _mediator = mediator;
        _logger = logger;

        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;

        UpdateAuthState();

        _logger.LogInformation("[AppHeader] Initialisiert - IsAuthenticated: {IsAuth}, CurrentTitle: {Title}",
            IsAuthenticated, CurrentTitle);
    }

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        _logger.LogInformation("[AppHeader] Auth State Changed: {IsAuth}", isAuthenticated);
        UpdateAuthState();
    }

    private void UpdateAuthState()
    {
        var wasAuthenticated = IsAuthenticated;
        IsAuthenticated = _authService.IsAuthenticated ? Visibility.Visible : Visibility.Collapsed;
        UserFullName = _authService.UserFullName;
        UserInitials = GetInitials(_authService.UserFullName);

        _logger.LogInformation("[AppHeader] UpdateAuthState - IsAuthenticated: {IsAuth}, IsNotAuthenticated: {IsNotAuth}, UserFullName: {Name}",
            IsAuthenticated, IsNotAuthenticated, UserFullName);
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

    [RelayCommand]
    private void Logout()
    {
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
    /// Handles the new PageHeaderChangedEvent - updates both title and header content
    /// </summary>
    public Task Handle(PageHeaderChangedEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        var newTitle = string.IsNullOrWhiteSpace(@event.Title) ? "HEIMATPLATZ" : @event.Title;
        _logger.LogInformation("[AppHeader] PageHeaderChangedEvent - Title: {Title}, HasContent: {HasContent}",
            newTitle, @event.HeaderContent != null);

        // Ensure property changes happen on UI thread - use App.MainWindow for Uno Platform
        var dispatcherQueue = App.MainWindow?.DispatcherQueue;
        if (dispatcherQueue != null)
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                CurrentTitle = newTitle;
                HeaderContent = @event.HeaderContent;
                _logger.LogInformation("[AppHeader] Updated - Title: {Title}, HeaderContent: {HasContent}",
                    CurrentTitle, HeaderContent != null);
            });
        }
        else
        {
            // Fallback: update directly (may work if already on UI thread)
            _logger.LogWarning("[AppHeader] DispatcherQueue is null, updating directly");
            CurrentTitle = newTitle;
            HeaderContent = @event.HeaderContent;
        }

        return Task.CompletedTask;
    }
}
