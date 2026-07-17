using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Notifications.Contracts.Mediator.Commands;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Events;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;
using ApiSellerType = Heimatplatz.Maui.ApiClient.Generated.SellerType;

namespace Heimatplatz.Maui.Features.Auth.Presentation;

/// <summary>
/// ViewModel fuer die Registrierungsseite.
/// Neues Modell: Jeder Account ist implizit Kaeufer. Optional kann der Benutzer
/// angeben, dass er auch verkaufen moechte (Privat/Makler/Verwaltung + Firmenname).
/// </summary>
[ShellMap<RegisterPage>("Register")]
public partial class RegisterViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IAuthService _authService;
    private readonly INavigator _navigator;
    private readonly ILogger<RegisterViewModel> _logger;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? BusyMessage { get; set; }

    [ObservableProperty]
    public partial string Vorname { get; set; }

    [ObservableProperty]
    public partial string Nachname { get; set; }

    [ObservableProperty]
    public partial string Email { get; set; }

    [ObservableProperty]
    public partial string Passwort { get; set; }

    [ObservableProperty]
    public partial string PasswortBestaetigung { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    /// <summary>True wenn der Benutzer auch Immobilien anbieten moechte</summary>
    [ObservableProperty]
    public partial bool WantsToSell { get; set; }

    [ObservableProperty]
    public partial bool IsPrivateSeller { get; set; }

    [ObservableProperty]
    public partial bool IsBrokerSeller { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NeedsCompanyName))]
    public partial bool IsPropertyManagerSeller { get; set; }

    [ObservableProperty]
    public partial string CompanyName { get; set; }

    /// <summary>True wenn ein Firmenname noetig ist (Makler oder Verwaltung)</summary>
    public bool NeedsCompanyName => IsBrokerSeller || IsPropertyManagerSeller;

    public RegisterViewModel(
        IMediator mediator,
        IAuthService authService,
        INavigator navigator,
        ILogger<RegisterViewModel> logger)
    {
        _mediator = mediator;
        _authService = authService;
        _navigator = navigator;
        _logger = logger;

        Vorname = string.Empty;
        Nachname = string.Empty;
        Email = string.Empty;
        Passwort = string.Empty;
        PasswortBestaetigung = string.Empty;
        CompanyName = string.Empty;
        IsPrivateSeller = true;
    }

    public bool CanRegister =>
        !string.IsNullOrWhiteSpace(Vorname) &&
        !string.IsNullOrWhiteSpace(Nachname) &&
        !string.IsNullOrWhiteSpace(Email) &&
        !string.IsNullOrWhiteSpace(Passwort) &&
        Passwort == PasswortBestaetigung &&
        Passwort.Length >= 8 &&
        (!WantsToSell || IsPrivateSeller || IsBrokerSeller || IsPropertyManagerSeller) &&
        (!WantsToSell || !NeedsCompanyName || !string.IsNullOrWhiteSpace(CompanyName));

    /// <summary>True wenn eine Fehlermeldung angezeigt werden soll</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));
    partial void OnWantsToSellChanged(bool value)
    {
        // Beim Deaktivieren: Verkaeufer-Felder zuruecksetzen
        if (!value)
        {
            IsPrivateSeller = true;
            IsBrokerSeller = false;
            IsPropertyManagerSeller = false;
            CompanyName = string.Empty;
        }
    }
    partial void OnIsPrivateSellerChanged(bool value)
    {
        if (value)
        {
            IsBrokerSeller = false;
            IsPropertyManagerSeller = false;
        }
        OnPropertyChanged(nameof(NeedsCompanyName));
    }
    partial void OnIsBrokerSellerChanged(bool value)
    {
        if (value)
        {
            IsPrivateSeller = false;
            IsPropertyManagerSeller = false;
        }
        OnPropertyChanged(nameof(NeedsCompanyName));
    }
    partial void OnIsPropertyManagerSellerChanged(bool value)
    {
        if (value)
        {
            IsPrivateSeller = false;
            IsBrokerSeller = false;
        }
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (!CanRegister)
        {
            ErrorMessage = GetValidationError();
            return;
        }

        ErrorMessage = null;
        IsBusy = true;
        BusyMessage = "Registrierung wird durchgeführt...";

        try
        {
            _logger.LogInformation("Registrierung fuer {Email}", Email);

            // SellerType nur setzen wenn der Benutzer verkaufen moechte
            ApiSellerType? sellerType = null;
            if (WantsToSell)
            {
                sellerType = IsBrokerSeller
                    ? ApiSellerType.Broker
                    : IsPropertyManagerSeller
                        ? ApiSellerType.PropertyManager
                        : ApiSellerType.Private;
            }

            // Der RegisterHttpRequest wird automatisch aus der OpenAPI-Spec generiert
            var response = await _mediator.Request(new RegisterHttpRequest
            {
                Body = new RegisterRequest
                {
                    FirstName = Vorname,
                    LastName = Nachname,
                    Email = Email,
                    Password = Passwort,
                    SellerType = sellerType,
                    CompanyName = WantsToSell && NeedsCompanyName ? CompanyName : null
                }
            });

            // Benutzer nach Registrierung automatisch einloggen
            _authService.SetAuthenticatedUser(
                response.Result.AccessToken,
                response.Result.RefreshToken,
                response.Result.UserId,
                response.Result.Email,
                response.Result.FullName,
                response.Result.ExpiresAt);

            _logger.LogInformation("Registrierung erfolgreich fuer {Email}", Email);

            // Login-Event fuer andere Features publizieren
            await _mediator.Publish(new UserLoggedInEvent(response.Result.UserId, response.Result.Email));

            // Push Notifications initialisieren (nur auf mobilen Plattformen verfuegbar)
            try
            {
                await _mediator.Send(new InitializePushNotificationsCommand());
            }
            catch (Exception pushEx)
            {
                _logger.LogWarning(pushEx, "Push Notifications konnten nicht initialisiert werden (nicht auf dieser Plattform verfuegbar)");
            }

            // Formular zuruecksetzen (insbesondere Passwoerter nicht im Speicher behalten)
            Vorname = string.Empty;
            Nachname = string.Empty;
            Email = string.Empty;
            Passwort = string.Empty;
            PasswortBestaetigung = string.Empty;
            CompanyName = string.Empty;

            // Absolute Navigation zur Root (Auth-Pages haben keinen Pop-History-Eintrag)
            await _navigator.NavigateTo("MainPage", relativeNavigation: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registrierung fehlgeschlagen fuer {Email}", Email);
            ErrorMessage = GetUserFriendlyErrorMessage(ex);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    [RelayCommand]
    private Task GoToLoginAsync() => _navigator.NavigateTo("Login");

    private string GetValidationError()
    {
        if (string.IsNullOrWhiteSpace(Vorname))
            return "Bitte geben Sie Ihren Vornamen ein.";
        if (string.IsNullOrWhiteSpace(Nachname))
            return "Bitte geben Sie Ihren Nachnamen ein.";
        if (string.IsNullOrWhiteSpace(Email))
            return "Bitte geben Sie Ihre E-Mail-Adresse ein.";
        if (string.IsNullOrWhiteSpace(Passwort))
            return "Bitte geben Sie ein Passwort ein.";
        if (Passwort.Length < 8)
            return "Das Passwort muss mindestens 8 Zeichen lang sein.";
        if (Passwort != PasswortBestaetigung)
            return "Die Passwörter stimmen nicht überein.";
        if (WantsToSell && !IsPrivateSeller && !IsBrokerSeller && !IsPropertyManagerSeller)
            return "Bitte wählen Sie einen Anbietertyp (Privat, Makler oder Verwaltung).";
        if (WantsToSell && NeedsCompanyName && string.IsNullOrWhiteSpace(CompanyName))
            return "Bitte geben Sie Ihren Firmennamen ein.";
        return string.Empty;
    }

    private static string GetUserFriendlyErrorMessage(Exception ex)
    {
        var message = ex.Message;

        // HTTP 409 Conflict - E-Mail bereits registriert
        if (message.Contains("409") || message.Contains("Conflict") || message.Contains("already exists") || message.Contains("bereits"))
            return "Diese E-Mail-Adresse ist bereits registriert.";

        // HTTP 400 Bad Request - Validierungsfehler
        if (message.Contains("400") || message.Contains("Bad Request") || message.Contains("validation"))
            return "Bitte überprüfen Sie Ihre Eingaben.";

        // HTTP 429 - Zu viele Versuche
        if (message.Contains("429") || message.Contains("Too Many"))
            return "Zu viele Versuche. Bitte warten Sie einen Moment.";

        // HTTP 500+ - Serverfehler
        if (message.Contains("500") || message.Contains("502") || message.Contains("503") ||
            message.Contains("Internal Server") || message.Contains("Bad Gateway") || message.Contains("Service Unavailable"))
            return "Der Server ist derzeit nicht erreichbar. Bitte versuchen Sie es später erneut.";

        // Netzwerkfehler
        if (message.Contains("net_http") || message.Contains("network") || message.Contains("connection") ||
            message.Contains("timeout") || message.Contains("Timeout") || message.Contains("SocketException") ||
            message.Contains("host") || message.Contains("DNS") || message.Contains("resolve"))
            return "Keine Internetverbindung. Bitte pruefen Sie Ihre Netzwerkverbindung.";

        // SSL/TLS Fehler
        if (message.Contains("SSL") || message.Contains("TLS") || message.Contains("certificate"))
            return "Sichere Verbindung fehlgeschlagen. Bitte pruefen Sie Ihre Netzwerkeinstellungen.";

        // Allgemeiner Fallback
        return "Registrierung fehlgeschlagen. Bitte versuchen Sie es erneut.";
    }
}
