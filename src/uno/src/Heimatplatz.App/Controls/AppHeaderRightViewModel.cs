using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace Heimatplatz.App.Controls;

/// <summary>
/// ViewModel fuer den rechten Header-Bereich.
/// Verwaltet ausschliesslich den Auth-Status und zeigt:
/// - User-Symbol mit Logout-Flyout wenn eingeloggt
/// - Anmelden/Registrieren Buttons wenn nicht eingeloggt
/// </summary>
public partial class AppHeaderRightViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ILogger<AppHeaderRightViewModel> _logger;

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

    public AppHeaderRightViewModel(
        IAuthService authService,
        ILogger<AppHeaderRightViewModel> logger)
    {
        _authService = authService;
        _logger = logger;

        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
        UpdateAuthState();

        _logger.LogInformation("[AppHeaderRight] Initialisiert - IsAuthenticated: {IsAuth}", IsAuthenticated);
    }

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        _logger.LogInformation("[AppHeaderRight] Auth State Changed: {IsAuth}", isAuthenticated);

        // Ensure property changes happen on UI thread
        var dispatcherQueue = App.MainWindow?.DispatcherQueue;
        if (dispatcherQueue != null)
        {
            dispatcherQueue.TryEnqueue(UpdateAuthState);
        }
        else
        {
            UpdateAuthState();
        }
    }

    private void UpdateAuthState()
    {
        IsAuthenticated = _authService.IsAuthenticated ? Visibility.Visible : Visibility.Collapsed;
        UserFullName = _authService.UserFullName;
        UserInitials = GetInitials(_authService.UserFullName);

        _logger.LogDebug("[AppHeaderRight] UpdateAuthState - IsAuthenticated: {IsAuth}, UserFullName: {Name}",
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
        _logger.LogInformation("[AppHeaderRight] Logout initiated");
        _authService.ClearAuthentication();
    }
}
