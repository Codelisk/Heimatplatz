using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Events;
using Heimatplatz.Maui.Localization.Auth;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Auth.Presentation;

/// <summary>
/// ViewModel fuer die Anmeldeseite (portiert aus der Uno-App)
/// </summary>
[ShellMap<LoginPage>("Login", registerRoute: false)]
public partial class LoginViewModel : ObservableObject, IPageLifecycleAware
{
    private readonly IMediator _mediator;
    private readonly IAuthService _authService;
    private readonly INavigator _navigator;
    private readonly ILogger<LoginViewModel> _logger;

    public LoginStringsLocalized Loc { get; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? BusyMessage { get; set; }

    [ObservableProperty]
    public partial string Email { get; set; }

    [ObservableProperty]
    public partial string Passwort { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public LoginViewModel(
        IMediator mediator,
        IAuthService authService,
        INavigator navigator,
        ILogger<LoginViewModel> logger,
        LoginStringsLocalized loc)
    {
        _mediator = mediator;
        _authService = authService;
        _navigator = navigator;
        _logger = logger;
        Loc = loc;

        Email = string.Empty;
        Passwort = string.Empty;
    }

    /// <summary>True wenn eine Fehlermeldung angezeigt werden soll</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    /// <summary>
    /// Die Login-Seite ist ein gecachtes Shell-Root: alte Fehlermeldung und
    /// eingegebenes Passwort duerfen bei erneutem Besuch nicht stehen bleiben.
    /// </summary>
    public void OnAppearing()
    {
        ErrorMessage = null;
        Passwort = string.Empty;
    }

    public void OnDisappearing()
    {
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        var validationError = GetValidationError();
        if (!string.IsNullOrEmpty(validationError))
        {
            ErrorMessage = validationError;
            return;
        }

        ErrorMessage = null;
        IsBusy = true;
        BusyMessage = Loc.BusyLoggingIn;

        try
        {
            _logger.LogInformation("Login-Versuch fuer {Email}", Email);

            var (_, result) = await _mediator.Request(new LoginHttpRequest
            {
                Body = new LoginRequest
                {
                    Email = Email,
                    Password = Passwort
                }
            });

            if (result == null)
            {
                ErrorMessage = Loc.LoginFailedRetry;
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

            // Login-Event fuer andere Features publizieren
            await _mediator.Publish(new UserLoggedInEvent(result.UserId, result.Email));

            // Formular zuruecksetzen
            Email = string.Empty;
            Passwort = string.Empty;

            // Absolute Navigation zur Root (Auth-Pages haben keinen Pop-History-Eintrag)
            await _navigator.NavigateTo("MainPage", relativeNavigation: false);
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

    [RelayCommand]
    private Task GoToRegisterAsync() => _navigator.NavigateTo("Register");

    [RelayCommand]
    private Task GoToForgotPasswordAsync() => _navigator.NavigateTo("ForgotPassword");

    // Nur ein grobes Format-Muster: massgeblich fuer die tatsaechliche
    // Gueltigkeit bleibt ausschliesslich die serverseitige Validierung.
    private static readonly Regex EmailFormatRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private string GetValidationError()
    {
        if (string.IsNullOrWhiteSpace(Email))
            return Loc.ValidationEmailRequired;
        if (!EmailFormatRegex.IsMatch(Email.Trim()))
            return Loc.ValidationEmailInvalid;
        if (string.IsNullOrWhiteSpace(Passwort))
            return Loc.ValidationPasswordRequired;
        return string.Empty;
    }

    private string GetUserFriendlyErrorMessage(Exception ex)
    {
        var message = ex.Message;

        // HTTP 401 Unauthorized - falsche Anmeldedaten
        if (message.Contains("401") || message.Contains("Unauthorized"))
            return Loc.ErrorWrongCredentials;

        // HTTP 403 Forbidden - Konto gesperrt oder keine Berechtigung
        if (message.Contains("403") || message.Contains("Forbidden"))
            return Loc.ErrorAccountLocked;

        // HTTP 404 - Benutzer nicht gefunden
        if (message.Contains("404") || message.Contains("Not Found"))
            return Loc.ErrorEmailNotRegistered;

        // HTTP 429 - Zu viele Versuche
        if (message.Contains("429") || message.Contains("Too Many"))
            return Loc.ErrorTooManyAttempts;

        // HTTP 500+ - Serverfehler
        if (message.Contains("500") || message.Contains("502") || message.Contains("503") ||
            message.Contains("Internal Server") || message.Contains("Bad Gateway") || message.Contains("Service Unavailable"))
            return Loc.ErrorServerUnavailable;

        // Netzwerkfehler (net_http, connection, timeout, etc.)
        if (message.Contains("net_http") || message.Contains("network") || message.Contains("connection") ||
            message.Contains("timeout") || message.Contains("Timeout") || message.Contains("SocketException") ||
            message.Contains("host") || message.Contains("DNS") || message.Contains("resolve"))
            return Loc.ErrorNoConnection;

        // SSL/TLS Fehler
        if (message.Contains("SSL") || message.Contains("TLS") || message.Contains("certificate"))
            return Loc.ErrorSslFailed;

        // Allgemeiner Fallback - keine technischen Details anzeigen
        return Loc.ErrorLoginFailed;
    }
}
