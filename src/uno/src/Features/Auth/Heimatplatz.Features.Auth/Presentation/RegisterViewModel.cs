using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Shiny.Mediator;

namespace Heimatplatz.Features.Auth.Presentation;

/// <summary>
/// ViewModel fuer die Registrierungsseite
/// Registered via Uno.Extensions.Navigation ViewMap (not [Service] attribute)
/// </summary>
public partial class RegisterViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IAuthService _authService;

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

    public RegisterViewModel(IMediator mediator, IAuthService authService)
    {
        _mediator = mediator;
        _authService = authService;
    }

    public bool CanRegister =>
        !string.IsNullOrWhiteSpace(Vorname) &&
        !string.IsNullOrWhiteSpace(Nachname) &&
        !string.IsNullOrWhiteSpace(Email) &&
        !string.IsNullOrWhiteSpace(Passwort) &&
        Passwort == PasswortBestaetigung &&
        Passwort.Length >= 8 &&
        (IsBuyer || IsSeller); // Mindestens eine Rolle muss ausgewaehlt sein

    partial void OnVornameChanged(string value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnNachnameChanged(string value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnEmailChanged(string value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnPasswortChanged(string value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnPasswortBestaetigungChanged(string value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnIsBuyerChanged(bool value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnIsSellerChanged(bool value) => OnPropertyChanged(nameof(CanRegister));

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
            // Rollen basierend auf Auswahl erstellen
            var selectedRoles = new List<Heimatplatz.Core.ApiClient.Generated.UserRoleType>();
            if (IsBuyer)
                selectedRoles.Add(Heimatplatz.Core.ApiClient.Generated.UserRoleType.Buyer);
            if (IsSeller)
                selectedRoles.Add(Heimatplatz.Core.ApiClient.Generated.UserRoleType.Seller);

            // Der RegisterHttpRequest wird automatisch aus der OpenAPI-Spec generiert
            // Body enthält den eigentlichen Request gemäß OpenAPI-Schema
            var response = await _mediator.Request(new Heimatplatz.Core.ApiClient.Generated.RegisterHttpRequest
            {
                Body = new Heimatplatz.Core.ApiClient.Generated.RegisterRequest
                {
                    Vorname = Vorname,
                    Nachname = Nachname,
                    Email = Email,
                    Passwort = Passwort,
                    Roles = selectedRoles
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

            IsSuccess = true;
            SuccessMessage = $"Willkommen, {response.Result.FullName}! Ihre Registrierung war erfolgreich.";

            // Formular zurücksetzen
            Vorname = string.Empty;
            Nachname = string.Empty;
            Email = string.Empty;
            Passwort = string.Empty;
            PasswortBestaetigung = string.Empty;
            IsBuyer = false;
            IsSeller = false;
        }
        catch (Exception ex)
        {
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
        return string.Empty;
    }
}
