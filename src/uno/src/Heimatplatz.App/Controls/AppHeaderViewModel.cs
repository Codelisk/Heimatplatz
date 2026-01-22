using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Events;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Services;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Shiny.Mediator;

namespace Heimatplatz.App.Controls;

/// <summary>
/// ViewModel fuer den AppHeader
/// Verwaltet den Authentifizierungsstatus und Logout-Funktionalitaet
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public partial class AppHeaderViewModel : ObservableObject, IEventHandler<PageTitleChangedEvent>
{
    private readonly IAuthService _authService;
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
        ILogger<AppHeaderViewModel> logger)
    {
        _authService = authService;
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
    /// Handles the PageTitleChangedEvent
    /// </summary>
    public Task Handle(PageTitleChangedEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[AppHeader] ===== PageTitleChangedEvent RECEIVED! Title: {Title} =====", @event.Title);
        _logger.LogInformation("[AppHeader] Before - CurrentTitle: {CurrentTitle}", CurrentTitle);

        // Ensure property change happens on UI thread
        Microsoft.UI.Xaml.Window.Current?.DispatcherQueue.TryEnqueue(() =>
        {
            CurrentTitle = @event.Title;
            _logger.LogInformation("[AppHeader] After - CurrentTitle: {CurrentTitle} (on UI thread)", CurrentTitle);
        });

        return Task.CompletedTask;
    }
}
