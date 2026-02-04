using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Notifications.Contracts.Mediator.Commands;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;
using Uno.Extensions.Navigation;

namespace Heimatplatz.Features.Auth.Presentation;

/// <summary>
/// ViewModel fuer die Registrierungsseite
/// Registered via Uno.Extensions.Navigation ViewMap (not [Service] attribute)
/// </summary>
public partial class RegisterViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IAuthService _authService;
    private readonly INavigator _navigator;
    private readonly ILogger<RegisterViewModel> _logger;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyMessage;

    [ObservableProperty]
    private string _vorname = string.Empty;

    [ObservableProperty]
    private string _nachname = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _passwort = string.Empty;

    [ObservableProperty]
    private string _passwortBestaetigung = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isSuccess;

    [ObservableProperty]
    private bool _isBuyer;

    [ObservableProperty]
    private bool _isSeller;

    [ObservableProperty]
    private string? _successMessage;

    [ObservableProperty]
    private bool _isPrivateSeller = true;

    [ObservableProperty]
    private bool _isBrokerSeller;

    [ObservableProperty]
    private string _companyName = string.Empty;

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

    partial void OnVornameChanged(string value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnNachnameChanged(string value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnEmailChanged(string value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnPasswortChanged(string value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnPasswortBestaetigungChanged(string value) => OnPropertyChanged(nameof(CanRegister));
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
            var selectedRoles = new List<Heimatplatz.Core.ApiClient.Generated.UserRoleType>();
            if (IsBuyer)
                selectedRoles.Add(Heimatplatz.Core.ApiClient.Generated.UserRoleType.Buyer);
            if (IsSeller)
                selectedRoles.Add(Heimatplatz.Core.ApiClient.Generated.UserRoleType.Seller);

            // SellerType bestimmen
            Heimatplatz.Core.ApiClient.Generated.SellerType? sellerType = null;
            if (IsSeller)
            {
                sellerType = IsPrivateSeller
                    ? Heimatplatz.Core.ApiClient.Generated.SellerType.Private
                    : Heimatplatz.Core.ApiClient.Generated.SellerType.Broker;
            }

            // Der RegisterHttpRequest wird automatisch aus der OpenAPI-Spec generiert
            var response = await _mediator.Request(new Heimatplatz.Core.ApiClient.Generated.RegisterHttpRequest
            {
                Body = new Heimatplatz.Core.ApiClient.Generated.RegisterRequest
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

            // Push Notifications initialisieren via Mediator Command
            await _mediator.Send(new InitializePushNotificationsCommand());

            // Zurueck zur HomePage navigieren
            await _navigator.NavigateBackAsync(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registrierung fehlgeschlagen fuer {Email}", Email);
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

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
}
