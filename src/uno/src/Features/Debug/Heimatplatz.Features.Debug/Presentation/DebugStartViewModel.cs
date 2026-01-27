#if DEBUG
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Debug.Services;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;
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
    private readonly IMediator _mediator;

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
        ILogger<DebugStartViewModel> logger,
        IMediator mediator)
    {
        _debugAuthService = debugAuthService;
        _authService = authService;
        _navigator = navigator;
        _logger = logger;
        _mediator = mediator;

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
        try
        {
            var success = await _debugAuthService.QuickLoginAsync(email, TestPassword);
            if (success)
            {
                _logger.LogInformation("[DEBUG] Login erfolgreich fuer {Email}", email);
            }
            else
            {
                _logger.LogWarning("[DEBUG] Login fehlgeschlagen fuer {Email} - API moeglicherweise nicht erreichbar", email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DEBUG] Exception beim Login fuer {Email}", email);
        }
        finally
        {
            // Immer UpdateUserInfo aufrufen, um UI zu aktualisieren
            UpdateUserInfo();
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _logger.LogInformation("[DEBUG] Logout - Publishing LogoutRequestedEvent");
        try
        {
            // Publish LogoutRequestedEvent which ShellViewModel handles
            await _mediator.Publish(new Heimatplatz.Events.LogoutRequestedEvent());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DEBUG] Exception beim Logout");
        }
        finally
        {
            UpdateUserInfo();
        }
    }

    [RelayCommand]
    private async Task NavigateToHomeAsync()
    {
        _logger.LogInformation("[DEBUG] Navigation zu Main (mit NavigationView)");
        await _navigator.NavigateRouteAsync(this, "Main");
    }

    [RelayCommand]
    private async Task NavigateToAddPropertyAsync()
    {
        _logger.LogInformation("[DEBUG] Navigation zu AddProperty");
        await _navigator.NavigateRouteAsync(this, "AddProperty");
    }

    [RelayCommand]
    private async Task NavigateToMyPropertiesAsync()
    {
        _logger.LogInformation("[DEBUG] Navigation zu MyProperties");
        await _navigator.NavigateRouteAsync(this, "MyProperties");
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
    private async Task NavigateToTestPushAsync()
    {
        _logger.LogInformation("[DEBUG] Navigation zu TestPush");
        await _navigator.NavigateRouteAsync(this, "TestPush");
    }

    [RelayCommand]
    private async Task EditFirstSellerPropertyAsync()
    {
        _logger.LogInformation("[DEBUG] Navigation zu EditProperty mit erster Seller-Property");

        try
        {
            // Ensure seller is logged in
            if (!_authService.IsAuthenticated || CurrentUserEmail != SellerEmail)
            {
                await QuickLoginAsync(SellerEmail);
            }

            // Fetch user's properties
            var result = await _mediator.Request(
                new Heimatplatz.Core.ApiClient.Generated.GetUserPropertiesHttpRequest()
            );

            if (result.Result?.Properties != null && result.Result.Properties.Count > 0)
            {
                // Get first property
                var firstProperty = result.Result.Properties[0];

                _logger.LogInformation("[DEBUG] Navigiere zu EditProperty mit PropertyId: {PropertyId}", firstProperty.Id);

                // Navigate to EditProperty with PropertyId
                await _navigator.NavigateRouteAsync(
                    this,
                    "EditProperty",
                    data: new Dictionary<string, object>
                    {
                        { "PropertyId", firstProperty.Id }
                    }
                );
            }
            else
            {
                _logger.LogWarning("[DEBUG] Keine Properties für Seller gefunden");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DEBUG] Fehler beim Laden der ersten Property");
        }
    }
}
#endif
