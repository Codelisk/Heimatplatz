using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Events;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Debug.Services;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Debug.Presentation;

/// <summary>
/// Debug-Werkzeuge fuer Entwicklungs-Builds: Testbenutzer anmelden/abmelden und zwischen
/// lokaler Entwicklungs-API, Test-API und Produktions-API umschalten.
/// </summary>
[ShellMap<DebugPage>("Debug", registerRoute: false)]
public partial class DebugViewModel : ObservableObject, IPageLifecycleAware
{
    private const string TestPassword = "Test123!";
    private const string BuyerEmail = "test.buyer@heimatplatz.dev";
    private const string SellerEmail = "test.seller@heimatplatz.dev";
    private const string BrokerEmail = "test.broker@heimatplatz.dev";
    private const string PropertyManagerEmail = "test.verwaltung@heimatplatz.dev";

    private readonly IApiEndpointService _apiEndpoints;
    private readonly IAuthService _authService;
    private readonly IMediator _mediator;
    private readonly ILogger<DebugViewModel> _logger;

    public DebugViewModel(
        IApiEndpointService apiEndpoints,
        IAuthService authService,
        IMediator mediator,
        ILogger<DebugViewModel> logger)
    {
        _apiEndpoints = apiEndpoints;
        _authService = authService;
        _mediator = mediator;
        _logger = logger;
        CurrentUrl = apiEndpoints.CurrentUrl;

        switch (apiEndpoints.SelectedEndpoint)
        {
            case ApiEndpointKind.Development:
                IsDevelopmentSelected = true;
                break;
            case ApiEndpointKind.Test:
                IsTestSelected = true;
                break;
            default:
                IsProductionSelected = true;
                break;
        }

        UpdateAuthenticationState();
    }

    [ObservableProperty]
    public partial bool IsDevelopmentSelected { get; set; }

    [ObservableProperty]
    public partial bool IsTestSelected { get; set; }

    [ObservableProperty]
    public partial bool IsProductionSelected { get; set; }

    [ObservableProperty]
    public partial string CurrentUrl { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    public partial bool IsBusy { get; set; }

    public bool IsNotBusy => !IsBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    [ObservableProperty]
    public partial string CurrentAuthenticationState { get; set; } = string.Empty;

    public string DevelopmentUrl => _apiEndpoints.DevelopmentUrl;

    public string TestUrl => _apiEndpoints.TestUrl;

    public string ProductionUrl => _apiEndpoints.ProductionUrl;

    public void OnAppearing()
    {
        ErrorMessage = null;
        UpdateAuthenticationState();
    }

    public void OnDisappearing()
    {
    }

    [RelayCommand]
    private void SetLoggedOut()
    {
        if (IsBusy)
            return;

        ErrorMessage = null;
        _authService.ClearAuthentication();
        UpdateAuthenticationState();
        _logger.LogInformation("Debug-Identitaet auf ausgeloggt gesetzt");
    }

    [RelayCommand]
    private Task LoginAsBuyerAsync() => LoginAsAsync(BuyerEmail, "Käufer");

    [RelayCommand]
    private Task LoginAsSellerAsync() => LoginAsAsync(SellerEmail, "Verkäufer (privat)");

    [RelayCommand]
    private Task LoginAsBrokerAsync() => LoginAsAsync(BrokerEmail, "Makler");

    [RelayCommand]
    private Task LoginAsPropertyManagerAsync() => LoginAsAsync(PropertyManagerEmail, "Hausverwaltung");

    partial void OnIsDevelopmentSelectedChanged(bool value)
    {
        if (value)
            Apply(ApiEndpointKind.Development);
    }

    partial void OnIsTestSelectedChanged(bool value)
    {
        if (value)
            Apply(ApiEndpointKind.Test);
    }

    partial void OnIsProductionSelectedChanged(bool value)
    {
        if (value)
            Apply(ApiEndpointKind.Production);
    }

    private void Apply(ApiEndpointKind kind)
    {
        _apiEndpoints.SetEndpoint(kind);
        CurrentUrl = _apiEndpoints.CurrentUrl;
    }

    private async Task LoginAsAsync(string email, string roleLabel)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var (_, result) = await _mediator.Request(new LoginHttpRequest
            {
                Body = new LoginRequest
                {
                    Email = email,
                    Password = TestPassword
                }
            });

            if (result is null)
            {
                ErrorMessage = $"Die Anmeldung als {roleLabel} hat keine Antwort geliefert.";
                return;
            }

            _authService.SetAuthenticatedUser(
                result.AccessToken,
                result.RefreshToken,
                result.UserId,
                result.Email,
                result.FullName,
                result.ExpiresAt);

            UpdateAuthenticationState();
            _logger.LogInformation("Debug-Anmeldung als {Role} ({Email}) erfolgreich", roleLabel, email);

            // Derselbe App-weite Login-Flow wie auf der regulaeren Anmeldeseite.
            await _mediator.Publish(new UserLoggedInEvent(result.UserId, result.Email));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Debug-Anmeldung als {Role} ({Email}) fehlgeschlagen", roleLabel, email);
            ErrorMessage = $"Anmeldung als {roleLabel} fehlgeschlagen. Läuft der gewählte API-Endpunkt und sind die Testbenutzer vorhanden?";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateAuthenticationState()
    {
        var sellerLabel = _authService.SellerType switch
        {
            "Broker" => "Verkäufer (Makler)",
            "PropertyManager" => "Verkäufer (Hausverwaltung)",
            "Private" => "Verkäufer (privat)",
            _ => "Verkäufer"
        };

        CurrentAuthenticationState = (_authService.IsAuthenticated, _authService.IsSeller, _authService.IsAdmin) switch
        {
            (false, _, _) => "Ausgeloggt",
            (true, _, true) => $"Admin — {_authService.UserEmail}",
            (true, true, false) => $"{sellerLabel} — {_authService.UserEmail}",
            // Kaeufer ist jedes Konto implizit
            (true, false, false) => $"Käufer — {_authService.UserEmail}"
        };
    }
}
