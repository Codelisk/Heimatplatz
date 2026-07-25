using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Events;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Debug.Services;
using Heimatplatz.Maui.Localization.Debug;
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

    /// <summary>
    /// Waehrend der Konstruktor die Radio-Zustaende vorbelegt, darf Apply() nicht
    /// feuern: Es wuerde die EFFEKTIVE Konfiguration (z.B. HEIMATPLATZ_API_URL-
    /// Env-Override) mit der persistierten Preference ueberschreiben - blosses
    /// Oeffnen der Debug-Seite hat so den Endpunkt zurueckgesetzt.
    /// </summary>
    private readonly bool _initializing;

    public DebugViewModel(
        IApiEndpointService apiEndpoints,
        IAuthService authService,
        IMediator mediator,
        ILogger<DebugViewModel> logger,
        DebugStringsLocalized loc)
    {
        Loc = loc;
        _apiEndpoints = apiEndpoints;
        _authService = authService;
        _mediator = mediator;
        _logger = logger;

        _initializing = true;
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
        _initializing = false;

        // Nach der Radio-Vorbelegung lesen: zeigt die tatsaechlich wirksame URL
        // (inkl. Env-Override), nicht die Preference
        CurrentUrl = apiEndpoints.CurrentUrl;

        UpdateAuthenticationState();
    }

    public DebugStringsLocalized Loc { get; }

    [ObservableProperty]
    public partial bool IsDevelopmentSelected { get; set; }

    [ObservableProperty]
    public partial bool IsTestSelected { get; set; }

    [ObservableProperty]
    public partial bool IsProductionSelected { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentUrlDisplay))]
    public partial string CurrentUrl { get; set; } = string.Empty;

    /// <summary>Anzeige "Aktiv: {Url}" unterhalb der Endpunkt-Auswahl</summary>
    public string CurrentUrlDisplay => Loc.ActiveUrlFormat(CurrentUrl);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    public partial bool IsBusy { get; set; }

    public bool IsNotBusy => !IsBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentAuthenticationStateDisplay))]
    public partial string CurrentAuthenticationState { get; set; } = string.Empty;

    /// <summary>Anzeige "Aktuell: {Status}" in der Anmeldestatus-Karte</summary>
    public string CurrentAuthenticationStateDisplay => Loc.CurrentStateFormat(CurrentAuthenticationState);

    public string DevelopmentLabel => Loc.DevelopmentEndpointFormat(_apiEndpoints.DevelopmentUrl);

    public string TestLabel => Loc.TestEndpointFormat(_apiEndpoints.TestUrl);

    public string ProductionLabel => Loc.ProductionEndpointFormat(_apiEndpoints.ProductionUrl);

    public void OnAppearing()
    {
        ErrorMessage = null;
        CurrentUrl = _apiEndpoints.CurrentUrl;
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
    private Task LoginAsBuyerAsync() => LoginAsAsync(BuyerEmail, Loc.RoleBuyer);

    [RelayCommand]
    private Task LoginAsSellerAsync() => LoginAsAsync(SellerEmail, Loc.RoleSellerPrivate);

    [RelayCommand]
    private Task LoginAsBrokerAsync() => LoginAsAsync(BrokerEmail, Loc.RoleBroker);

    [RelayCommand]
    private Task LoginAsPropertyManagerAsync() => LoginAsAsync(PropertyManagerEmail, Loc.RolePropertyManager);

    partial void OnIsDevelopmentSelectedChanged(bool value)
    {
        if (value && !_initializing)
            Apply(ApiEndpointKind.Development);
    }

    partial void OnIsTestSelectedChanged(bool value)
    {
        if (value && !_initializing)
            Apply(ApiEndpointKind.Test);
    }

    partial void OnIsProductionSelectedChanged(bool value)
    {
        if (value && !_initializing)
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
                ErrorMessage = Loc.LoginNoResponseFormat(roleLabel);
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
            ErrorMessage = Loc.LoginFailedFormat(roleLabel);
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
            "Broker" => Loc.RoleSellerBroker,
            "PropertyManager" => Loc.RoleSellerPropertyManager,
            "Private" => Loc.RoleSellerPrivate,
            _ => Loc.RoleSeller
        };

        CurrentAuthenticationState = (_authService.IsAuthenticated, _authService.IsSeller, _authService.IsAdmin) switch
        {
            (false, _, _) => Loc.StateLoggedOut,
            (true, _, true) => Loc.StateAdminFormat(_authService.UserEmail),
            (true, true, false) => Loc.StateUserFormat(sellerLabel, _authService.UserEmail),
            // Kaeufer ist jedes Konto implizit
            (true, false, false) => Loc.StateUserFormat(Loc.RoleBuyer, _authService.UserEmail)
        };
    }
}
