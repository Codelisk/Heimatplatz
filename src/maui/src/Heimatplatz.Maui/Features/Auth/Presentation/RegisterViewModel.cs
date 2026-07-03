using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Notifications.Contracts.Mediator.Commands;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Events;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;
// Aliase: UserRoleType existiert auch in Heimatplatz.Maui.Features.Auth (IAuthService.cs)
// und wuerde durch den umschliessenden Namespace faelschlich bevorzugt werden.
using ApiSellerType = Heimatplatz.Maui.ApiClient.Generated.SellerType;
using ApiUserRoleType = Heimatplatz.Maui.ApiClient.Generated.UserRoleType;

namespace Heimatplatz.Maui.Features.Auth.Presentation;

/// <summary>
/// ViewModel fuer die Registrierungsseite (portiert aus der Uno-App)
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

    [ObservableProperty]
    public partial bool IsSuccess { get; set; }

    [ObservableProperty]
    public partial bool IsBuyer { get; set; }

    [ObservableProperty]
    public partial bool IsSeller { get; set; }

    [ObservableProperty]
    public partial string? SuccessMessage { get; set; }

    [ObservableProperty]
    public partial bool IsPrivateSeller { get; set; }

    [ObservableProperty]
    public partial bool IsBrokerSeller { get; set; }

    [ObservableProperty]
    public partial string CompanyName { get; set; }

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
        (IsBuyer || IsSeller) && // Mindestens eine Rolle muss ausgewaehlt sein
        (!IsSeller || IsPrivateSeller || IsBrokerSeller) && // Wenn Seller, muss Typ gewaehlt sein
        (!IsBrokerSeller || !string.IsNullOrWhiteSpace(CompanyName)); // Wenn Broker, muss Firmenname angegeben sein

    /// <summary>True wenn eine Fehlermeldung angezeigt werden soll</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnVornameChanged(string value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnNachnameChanged(string value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnEmailChanged(string value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnPasswortChanged(string value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnPasswortBestaetigungChanged(string value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));
    partial void OnIsBuyerChanged(bool value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnIsSellerChanged(bool value)
    {
        OnPropertyChanged(nameof(CanRegister));
        // Beim Deaktivieren von Seller: SellerType-Felder zuruecksetzen
        if (!value)
        {
            IsPrivateSeller = true;
            IsBrokerSeller = false;
            CompanyName = string.Empty;
        }
    }
    partial void OnIsPrivateSellerChanged(bool value)
    {
        if (value) IsBrokerSeller = false;
        OnPropertyChanged(nameof(CanRegister));
    }
    partial void OnIsBrokerSellerChanged(bool value)
    {
        if (value) IsPrivateSeller = false;
        OnPropertyChanged(nameof(CanRegister));
    }
    partial void OnCompanyNameChanged(string value) => OnPropertyChanged(nameof(CanRegister));

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (!CanRegister)
        {
            ErrorMessage = GetValidationError();
            return;
        }

        ErrorMessage = null;
        IsSuccess = false;
        IsBusy = true;
        BusyMessage = "Registrierung wird durchgeführt...";

        try
        {
            _logger.LogInformation("Registrierung fuer {Email}", Email);

            // Rollen basierend auf Auswahl erstellen
            var selectedRoles = new List<ApiUserRoleType>();
            if (IsBuyer)
                selectedRoles.Add(ApiUserRoleType.Buyer);
            if (IsSeller)
                selectedRoles.Add(ApiUserRoleType.Seller);

            // SellerType bestimmen
            ApiSellerType? sellerType = null;
            if (IsSeller)
            {
                sellerType = IsPrivateSeller
                    ? ApiSellerType.Private
                    : ApiSellerType.Broker;
            }

            // Der RegisterHttpRequest wird automatisch aus der OpenAPI-Spec generiert
            var response = await _mediator.Request(new RegisterHttpRequest
            {
                Body = new RegisterRequest
                {
                    Vorname = Vorname,
                    Nachname = Nachname,
                    Email = Email,
                    Passwort = Passwort,
                    Roles = selectedRoles,
                    SellerType = sellerType,
                    CompanyName = IsBrokerSeller ? CompanyName : null
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
        if (!IsBuyer && !IsSeller)
            return "Bitte wählen Sie mindestens eine Rolle (Käufer oder Verkäufer).";
        if (IsSeller && !IsPrivateSeller && !IsBrokerSeller)
            return "Bitte wählen Sie einen Verkäufertyp (Privatperson oder Makler).";
        if (IsBrokerSeller && string.IsNullOrWhiteSpace(CompanyName))
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
