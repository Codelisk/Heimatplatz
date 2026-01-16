using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Features.Auth.Presentation;

/// <summary>
/// ViewModel fuer die Registrierungsseite
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
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
        Passwort.Length >= 8;

    partial void OnVornameChanged(string value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnNachnameChanged(string value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnEmailChanged(string value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnPasswortChanged(string value) => OnPropertyChanged(nameof(CanRegister));
    partial void OnPasswortBestaetigungChanged(string value) => OnPropertyChanged(nameof(CanRegister));

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
            // Der RegisterHttpRequest wird automatisch aus der OpenAPI-Spec generiert
            // Body enthält den eigentlichen Request gemäß OpenAPI-Schema
            var response = await _mediator.Request(new Heimatplatz.Core.ApiClient.Generated.RegisterHttpRequest
            {
                Body = new Heimatplatz.Core.ApiClient.Generated.RegisterRequest
                {
                    Vorname = Vorname,
                    Nachname = Nachname,
                    Email = Email,
                    Passwort = Passwort
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
        return string.Empty;
    }
}
