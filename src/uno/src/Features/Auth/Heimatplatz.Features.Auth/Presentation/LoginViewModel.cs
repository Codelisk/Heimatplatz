using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Core.ApiClient.Generated;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Notifications.Contracts.Mediator.Commands;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;
using Uno.Extensions.Navigation;

namespace Heimatplatz.Features.Auth.Presentation;

/// <summary>
/// ViewModel fuer die Anmeldeseite
/// Registered via Uno.Extensions.Navigation ViewMap (not [Service] attribute)
/// </summary>
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

            // Push Notifications initialisieren (nur auf mobilen Plattformen verfuegbar)
            try
            {
                await _mediator.Send(new InitializePushNotificationsCommand());
            }
            catch (Exception pushEx)
            {
                _logger.LogWarning(pushEx, "Push Notifications konnten nicht initialisiert werden (nicht auf dieser Plattform verfuegbar)");
            }

            // Formular zuruecksetzen
            Email = string.Empty;
            Passwort = string.Empty;

            // Zurueck zur HomePage navigieren
            await _navigator.NavigateBackAsync(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login fehlgeschlagen fuer {Email}", Email);
            ErrorMessage = GetUserFriendlyErrorMessage(ex);
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

    private static string GetUserFriendlyErrorMessage(Exception ex)
    {
        var message = ex.Message;

        // HTTP 401 Unauthorized - falsche Anmeldedaten
        if (message.Contains("401") || message.Contains("Unauthorized"))
            return "E-Mail-Adresse oder Passwort ist falsch.";

        // HTTP 403 Forbidden - Konto gesperrt oder keine Berechtigung
        if (message.Contains("403") || message.Contains("Forbidden"))
            return "Ihr Konto ist gesperrt. Bitte kontaktieren Sie den Support.";

        // HTTP 404 - Benutzer nicht gefunden
        if (message.Contains("404") || message.Contains("Not Found"))
            return "Diese E-Mail-Adresse ist nicht registriert.";

        // HTTP 429 - Zu viele Versuche
        if (message.Contains("429") || message.Contains("Too Many"))
            return "Zu viele Anmeldeversuche. Bitte warten Sie einen Moment.";

        // HTTP 500+ - Serverfehler
        if (message.Contains("500") || message.Contains("502") || message.Contains("503") ||
            message.Contains("Internal Server") || message.Contains("Bad Gateway") || message.Contains("Service Unavailable"))
            return "Der Server ist derzeit nicht erreichbar. Bitte versuchen Sie es später erneut.";

        // Netzwerkfehler (net_http, connection, timeout, etc.)
        if (message.Contains("net_http") || message.Contains("network") || message.Contains("connection") ||
            message.Contains("timeout") || message.Contains("Timeout") || message.Contains("SocketException") ||
            message.Contains("host") || message.Contains("DNS") || message.Contains("resolve"))
            return "Keine Internetverbindung. Bitte pruefen Sie Ihre Netzwerkverbindung.";

        // SSL/TLS Fehler
        if (message.Contains("SSL") || message.Contains("TLS") || message.Contains("certificate"))
            return "Sichere Verbindung fehlgeschlagen. Bitte pruefen Sie Ihre Netzwerkeinstellungen.";

        // Allgemeiner Fallback - keine technischen Details anzeigen
        return "Anmeldung fehlgeschlagen. Bitte versuchen Sie es erneut.";
    }
}
