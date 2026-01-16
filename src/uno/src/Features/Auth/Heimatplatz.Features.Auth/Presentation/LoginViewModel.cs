using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Core.ApiClient.Generated;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;
using Uno.Extensions.Navigation;

namespace Heimatplatz.Features.Auth.Presentation;

/// <summary>
/// ViewModel fuer die Anmeldeseite
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public partial class LoginViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IAuthService _authService;
    private readonly INavigator _navigator;
    private readonly ILogger<LoginViewModel> _logger;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyMessage;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _passwort = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    public LoginViewModel(
        IMediator mediator,
        IAuthService authService,
        INavigator navigator,
        ILogger<LoginViewModel> logger)
    {
        _mediator = mediator;
        _authService = authService;
        _navigator = navigator;
        _logger = logger;
    }

    public bool CanLogin =>
        !string.IsNullOrWhiteSpace(Email) &&
        !string.IsNullOrWhiteSpace(Passwort);

    partial void OnEmailChanged(string value) => OnPropertyChanged(nameof(CanLogin));
    partial void OnPasswortChanged(string value) => OnPropertyChanged(nameof(CanLogin));

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (!CanLogin)
        {
            ErrorMessage = GetValidationError();
            return;
        }

        ErrorMessage = null;
        IsBusy = true;
        BusyMessage = "Anmeldung wird durchgeführt...";

        try
        {
            _logger.LogInformation("Login-Versuch fuer {Email}", Email);

            var (_, result) = await _mediator.Request(new LoginHttpRequest
            {
                Body = new LoginRequest
                {
                    Email = Email,
                    Passwort = Passwort
                }
            });

            if (result == null)
            {
                ErrorMessage = "Login fehlgeschlagen. Bitte versuchen Sie es erneut.";
                return;
            }

            // Token und Benutzerdaten speichern
            _authService.SetAuthenticatedUser(
                result.AccessToken,
                result.RefreshToken,
                result.UserId,
                result.Email,
                result.FullName,
                result.ExpiresAt);

            _logger.LogInformation("Login erfolgreich fuer {Email}", Email);

            // Formular zuruecksetzen
            Email = string.Empty;
            Passwort = string.Empty;

            // Zurueck zur HomePage navigieren
            await _navigator.NavigateBackAsync(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login fehlgeschlagen fuer {Email}", Email);
            ErrorMessage = ex.Message.Contains("401") || ex.Message.Contains("Unauthorized")
                ? "Ungueltige E-Mail-Adresse oder Passwort."
                : $"Login fehlgeschlagen: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    private string GetValidationError()
    {
        if (string.IsNullOrWhiteSpace(Email))
            return "Bitte geben Sie Ihre E-Mail-Adresse ein.";
        if (string.IsNullOrWhiteSpace(Passwort))
            return "Bitte geben Sie Ihr Passwort ein.";
        return string.Empty;
    }
}
