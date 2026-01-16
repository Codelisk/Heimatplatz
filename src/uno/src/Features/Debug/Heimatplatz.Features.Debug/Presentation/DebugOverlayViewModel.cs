#if DEBUG
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Debug.Services;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Shiny.Extensions.DependencyInjection;
using Uno.Extensions.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace Heimatplatz.Features.Debug.Presentation;

/// <summary>
/// ViewModel fuer das Debug-Overlay
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public partial class DebugOverlayViewModel : ObservableObject
{
    private readonly IDebugAuthService _debugAuthService;
    private readonly IAuthService _authService;
    private readonly INavigator _navigator;
    private readonly ILogger<DebugOverlayViewModel> _logger;

    // Test-User Credentials
    private const string BuyerEmail = "test.buyer@heimatplatz.dev";
    private const string SellerEmail = "test.seller@heimatplatz.dev";
    private const string BothEmail = "test.both@heimatplatz.dev";
    private const string TestPassword = "Test123!";

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string _currentUserEmail = "Nicht angemeldet";

    [ObservableProperty]
    private string _currentUserRoles = "-";

    public DebugOverlayViewModel(
        IDebugAuthService debugAuthService,
        IAuthService authService,
        INavigator navigator,
        ILogger<DebugOverlayViewModel> logger)
    {
        _debugAuthService = debugAuthService;
        _authService = authService;
        _navigator = navigator;
        _logger = logger;

        // Auth-State-Changes beobachten
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
        UpdateUserInfo();
    }

    public Visibility HasStatusMessage =>
        string.IsNullOrEmpty(StatusMessage) ? Visibility.Collapsed : Visibility.Visible;

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        UpdateUserInfo();
    }

    private void UpdateUserInfo()
    {
        CurrentUserEmail = _debugAuthService.GetCurrentUserEmail() ?? "Nicht angemeldet";
        CurrentUserRoles = _debugAuthService.GetCurrentUserRoles();
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    [RelayCommand]
    private async Task LoginAsBuyerAsync()
    {
        await QuickLoginAsync(BuyerEmail, "Buyer");
    }

    [RelayCommand]
    private async Task LoginAsSellerAsync()
    {
        await QuickLoginAsync(SellerEmail, "Seller");
    }

    [RelayCommand]
    private async Task LoginAsBothAsync()
    {
        await QuickLoginAsync(BothEmail, "Both");
    }

    private async Task QuickLoginAsync(string email, string roleName)
    {
        _logger.LogInformation("[DEBUG] Quick Login als {Role}", roleName);
        StatusMessage = $"Login als {roleName}...";

        var success = await _debugAuthService.QuickLoginAsync(email, TestPassword);

        if (success)
        {
            StatusMessage = $"✓ Erfolgreich als {roleName} angemeldet";
            UpdateUserInfo();

            // Status-Message nach 3 Sekunden ausblenden
            _ = Task.Delay(3000).ContinueWith(_ => StatusMessage = null);
        }
        else
        {
            StatusMessage = $"✗ Login als {roleName} fehlgeschlagen";
            _logger.LogWarning("[DEBUG] Login fehlgeschlagen");
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _logger.LogInformation("[DEBUG] Logout");
        await _debugAuthService.LogoutAsync();
        StatusMessage = "✓ Abgemeldet";
        UpdateUserInfo();

        // Status-Message nach 3 Sekunden ausblenden
        _ = Task.Delay(3000).ContinueWith(_ => StatusMessage = null);
    }

    [RelayCommand]
    private async Task NavigateToHomeAsync()
    {
        _logger.LogInformation("[DEBUG] Navigation zu Home");
        await _navigator.NavigateRouteAsync(this, "");
    }

    [RelayCommand]
    private async Task NavigateToAddPropertyAsync()
    {
        _logger.LogInformation("[DEBUG] Navigation zu AddProperty");
        await _navigator.NavigateRouteAsync(this, "AddProperty");
    }

    [RelayCommand]
    private async Task NavigateToLoginAsync()
    {
        _logger.LogInformation("[DEBUG] Navigation zu Login");
        await _navigator.NavigateRouteAsync(this, "Login");
    }

    [RelayCommand]
    private async Task NavigateToRegisterAsync()
    {
        _logger.LogInformation("[DEBUG] Navigation zu Register");
        await _navigator.NavigateRouteAsync(this, "Register");
    }

    [RelayCommand]
    private void CopyToken()
    {
        var token = _debugAuthService.GetCurrentToken();
        if (!string.IsNullOrEmpty(token))
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(token);
            Clipboard.SetContent(dataPackage);

            StatusMessage = "✓ Token in Zwischenablage kopiert";
            _logger.LogInformation("[DEBUG] Token kopiert");

            // Status-Message nach 3 Sekunden ausblenden
            _ = Task.Delay(3000).ContinueWith(_ => StatusMessage = null);
        }
        else
        {
            StatusMessage = "✗ Kein Token vorhanden";
        }
    }
}
#endif
