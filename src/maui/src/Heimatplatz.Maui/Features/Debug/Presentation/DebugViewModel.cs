using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Core.Build;
using Heimatplatz.Maui.Events;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Debug.Services;
using Heimatplatz.Maui.Localization.Debug;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Debug.Presentation;

/// <summary>
/// Debug-Werkzeuge fuer Entwicklungs- und interne Testversionen (Play-Test-Tracks,
/// TestFlight): Testbenutzer anmelden/abmelden und zwischen lokaler Entwicklungs-API,
/// Test-API und Produktions-API umschalten. In der Store-Version ist die Seite nicht
/// erreichbar - der Flyout-Eintrag wird dort nicht angelegt (siehe AppShell).
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
    private readonly INavigator _navigator;
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
        INavigator navigator,
        ILogger<DebugViewModel> logger,
        DebugStringsLocalized loc)
    {
        Loc = loc;
        _apiEndpoints = apiEndpoints;
        _authService = authService;
        _mediator = mediator;
        _navigator = navigator;
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

        UpdateEndpointDependentState();
        UpdateAuthenticationState();
    }

    /// <summary>Kanal dieses Builds, z.B. "TestFlight" oder "Interner Test"</summary>
    public string ChannelDisplay => Loc.ChannelFormat(AppChannels.DisplayName);

    /// <summary>
    /// Die lokale Entwicklungs-API ist nur am Entwicklungsrechner erreichbar - auf einem
    /// TestFlight-/Play-Test-Geraet waere die Option nur eine Sackgasse.
    /// </summary>
    public bool IsDevelopmentEndpointAvailable => ApiEndpoints.IsDevelopmentEndpointAvailable;

    /// <summary>
    /// Die Testkonten existieren nur in der Entwicklungs- und Test-Datenbank. Gegen die
    /// Produktions-API werden die Schnellanmeldungen deshalb ausgeblendet.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreTestLoginsUnavailable))]
    public partial bool AreTestLoginsAvailable { get; set; }

    public bool AreTestLoginsUnavailable => !AreTestLoginsAvailable;

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

    /// <summary>Bestaetigung nach einem Umgebungswechsel (inkl. Hinweis auf die Abmeldung)</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string? StatusMessage { get; set; }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

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
        UpdateEndpointDependentState();
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

    [RelayCommand]
    private Task OpenMapLibreSpikeAsync() => _navigator.NavigateTo<MapLibreSpikeViewModel>();

    partial void OnIsDevelopmentSelectedChanged(bool value)
    {
        if (value && !_initializing)
            _ = ApplyAsync(ApiEndpointKind.Development);
    }

    partial void OnIsTestSelectedChanged(bool value)
    {
        if (value && !_initializing)
            _ = ApplyAsync(ApiEndpointKind.Test);
    }

    partial void OnIsProductionSelectedChanged(bool value)
    {
        if (value && !_initializing)
            _ = ApplyAsync(ApiEndpointKind.Production);
    }

    /// <summary>
    /// Umgebungswechsel: Endpunkt umstellen, Session beenden und offene Listen neu laden
    /// (siehe ApiEndpointService.SwitchEndpointAsync). Der Aufruf kommt aus einem
    /// Property-Setter des RadioButtons, deshalb ohne await gestartet - Fehler landen
    /// im Fehlerbanner statt in einer unbeobachteten Task.
    /// </summary>
    private async Task ApplyAsync(ApiEndpointKind kind)
    {
        ErrorMessage = null;
        StatusMessage = null;

        try
        {
            var loggedOut = await _apiEndpoints.SwitchEndpointAsync(kind);

            CurrentUrl = _apiEndpoints.CurrentUrl;
            UpdateEndpointDependentState();
            UpdateAuthenticationState();

            var endpointName = ApiEndpoints.GetDisplayName(kind);
            StatusMessage = loggedOut
                ? Loc.SwitchedWithLogoutFormat(endpointName)
                : Loc.SwitchedFormat(endpointName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wechsel auf den Endpunkt {Endpoint} fehlgeschlagen", kind);
            ErrorMessage = Loc.SwitchFailed;
        }
    }

    /// <summary>
    /// Zustaende, die vom aktiven Endpunkt abhaengen (aktuell: Verfuegbarkeit der
    /// Schnellanmeldungen mit Testkonten)
    /// </summary>
    private void UpdateEndpointDependentState() =>
        AreTestLoginsAvailable =
            ApiEndpoints.GetKindForUrl(_apiEndpoints.CurrentUrl) != ApiEndpointKind.Production;

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
