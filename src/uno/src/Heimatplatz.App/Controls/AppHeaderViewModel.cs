using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace Heimatplatz.App.Controls;

/// <summary>
/// ViewModel fuer den AppHeader
/// Verwaltet den Authentifizierungsstatus und Logout-Funktionalitaet
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public partial class AppHeaderViewModel : ObservableObject
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

    public AppHeaderViewModel(
        IAuthService authService,
        ILogger<AppHeaderViewModel> logger)
    {
        _authService = authService;
        _logger = logger;
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
        UpdateAuthState();

        _logger.LogInformation("[AppHeader] Initialisiert - IsAuthenticated: {IsAuth}", IsAuthenticated);
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
}
