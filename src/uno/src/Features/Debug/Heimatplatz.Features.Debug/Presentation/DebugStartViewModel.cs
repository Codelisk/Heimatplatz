#if DEBUG
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Debug.Services;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Uno.Extensions.Navigation;

namespace Heimatplatz.Features.Debug.Presentation;

/// <summary>
/// ViewModel fuer die Debug-Start-Page
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public partial class DebugStartViewModel : ObservableObject
{
    private readonly IDebugAuthService _debugAuthService;
    private readonly IAuthService _authService;
    private readonly INavigator _navigator;
    private readonly ILogger<DebugStartViewModel> _logger;

    // Test-User Credentials
    private const string BuyerEmail = "test.buyer@heimatplatz.dev";
    private const string SellerEmail = "test.seller@heimatplatz.dev";
    private const string BothEmail = "test.both@heimatplatz.dev";
    private const string TestPassword = "Test123!";

    [ObservableProperty]
    private bool _isLoggedOut = true;

    [ObservableProperty]
    private bool _isBuyer;

    [ObservableProperty]
    private bool _isSeller;

    [ObservableProperty]
    private bool _isBoth;

    [ObservableProperty]
    private string _currentUserEmail = "Ausgeloggt";

    [ObservableProperty]
    private string _currentUserRoles = "-";

    public DebugStartViewModel(
        IDebugAuthService debugAuthService,
        IAuthService authService,
        INavigator navigator,
        ILogger<DebugStartViewModel> logger)
    {
        _debugAuthService = debugAuthService;
        _authService = authService;
        _navigator = navigator;
        _logger = logger;

        // Auth-State-Changes beobachten
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
        UpdateUserInfo();
    }

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        UpdateUserInfo();
    }

    private void UpdateUserInfo()
    {
        CurrentUserEmail = _debugAuthService.GetCurrentUserEmail() ?? "Ausgeloggt";
        CurrentUserRoles = _debugAuthService.GetCurrentUserRoles();

        // Update toggle states
        var isAuth = _authService.IsAuthenticated;
        IsLoggedOut = !isAuth;
        IsBuyer = isAuth && CurrentUserEmail == BuyerEmail;
        IsSeller = isAuth && CurrentUserEmail == SellerEmail;
        IsBoth = isAuth && CurrentUserEmail == BothEmail;
    }

    [RelayCommand]
    private async Task LoginAsBuyerAsync()
    {
        _logger.LogInformation("[DEBUG] Login als Buyer");
        await QuickLoginAsync(BuyerEmail);
    }

    [RelayCommand]
    private async Task LoginAsSellerAsync()
    {
        _logger.LogInformation("[DEBUG] Login als Seller");
        await QuickLoginAsync(SellerEmail);
    }

    [RelayCommand]
    private async Task LoginAsBothAsync()
    {
        _logger.LogInformation("[DEBUG] Login als Both");
        await QuickLoginAsync(BothEmail);
    }

    private async Task QuickLoginAsync(string email)
    {
        var success = await _debugAuthService.QuickLoginAsync(email, TestPassword);
        if (success)
        {
            UpdateUserInfo();
        }
        else
        {
            _logger.LogWarning("[DEBUG] Login fehlgeschlagen fuer {Email}", email);
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _logger.LogInformation("[DEBUG] Logout");
        await _debugAuthService.LogoutAsync();
        UpdateUserInfo();
    }

    [RelayCommand]
    private async Task NavigateToHomeAsync()
    {
        _logger.LogInformation("[DEBUG] Navigation zu Home");
        await _navigator.NavigateRouteAsync(this, "Home");
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
}
#endif
