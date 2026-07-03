using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Events;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Auth.Presentation;

/// <summary>
/// ViewModel fuer die Mein Profil Seite (portiert aus der Uno-App).
/// Zeigt alle verfuegbaren Benutzerdaten aus dem IAuthService an, bietet Logout
/// und die Moeglichkeit, das Konto endgueltig zu loeschen (Apple Guideline 5.1.1(v)).
/// </summary>
[ShellMap<UserProfilePage>("UserProfile")]
public partial class UserProfileViewModel : ObservableObject, IPageLifecycleAware
{
    private readonly IAuthService _authService;
    private readonly IMediator _mediator;
    private readonly INavigator _navigator;
    private readonly IDialogs _dialogs;
    private readonly ILogger<UserProfileViewModel> _logger;

    [ObservableProperty]
    public partial string UserFullName { get; set; }

    [ObservableProperty]
    public partial string UserInitials { get; set; }

    [ObservableProperty]
    public partial string UserEmail { get; set; }

    [ObservableProperty]
    public partial string RoleBadgeText { get; set; }

    [ObservableProperty]
    public partial bool IsSeller { get; set; }

    [ObservableProperty]
    public partial bool IsBuyer { get; set; }

    /// <summary>True waehrend die Konto-Loeschung laeuft (zeigt Ladeindikator, blockiert UI).</summary>
    [ObservableProperty]
    public partial bool IsDeletingAccount { get; set; }

    public UserProfileViewModel(
        IAuthService authService,
        IMediator mediator,
        INavigator navigator,
        IDialogs dialogs,
        ILogger<UserProfileViewModel> logger)
    {
        _authService = authService;
        _mediator = mediator;
        _navigator = navigator;
        _dialogs = dialogs;
        _logger = logger;

        UserFullName = string.Empty;
        UserInitials = string.Empty;
        UserEmail = string.Empty;
        RoleBadgeText = string.Empty;

        LoadUserData();
    }

    public void OnAppearing()
    {
        _logger.LogDebug("[UserProfile] OnAppearing");

        // Nicht angemeldete Nutzer zur Anmeldung weiterleiten - der Login-ShellContent
        // ist im Flyout versteckt, "Profil" ist damit der Einstieg zum Login
        // (Pendant zum "Anmelden"-Button im AppHeader der Uno-App).
        if (!_authService.IsAuthenticated)
        {
            _ = _navigator.NavigateTo("Login", relativeNavigation: false);
            return;
        }

        LoadUserData();
    }

    public void OnDisappearing()
    {
        _logger.LogDebug("[UserProfile] OnDisappearing");
    }

    private void LoadUserData()
    {
        if (!_authService.IsAuthenticated)
        {
            _logger.LogWarning("[UserProfile] Nicht authentifiziert");
            return;
        }

        UserFullName = _authService.UserFullName ?? string.Empty;
        UserEmail = _authService.UserEmail ?? string.Empty;
        UserInitials = GetInitials(_authService.UserFullName);
        IsSeller = _authService.IsSeller;
        IsBuyer = _authService.IsBuyer;

        // Role badge text
        RoleBadgeText = (IsSeller, IsBuyer) switch
        {
            (true, true) => "Käufer & Verkäufer",
            (true, false) => "Verkäufer",
            (false, true) => "Käufer",
            _ => string.Empty
        };

        _logger.LogInformation("[UserProfile] Benutzerdaten geladen: {Name}, {Email}", UserFullName, UserEmail);
    }

    /// <summary>
    /// Meldet den Benutzer ab: Auth-Daten loeschen, LogoutRequestedEvent publizieren
    /// und absolut zur Login-Seite navigieren.
    /// </summary>
    [RelayCommand]
    private async Task LogoutAsync()
    {
        _logger.LogInformation("[UserProfile] Logout angefordert");

        _authService.ClearAuthentication();
        await _mediator.Publish(new LogoutRequestedEvent());
        await _navigator.NavigateTo("Login", relativeNavigation: false);
    }

    /// <summary>
    /// Loescht das Konto des Benutzers nach einer ausdruecklichen Bestaetigung.
    /// Die eigentliche Loeschlogik liegt vollstaendig im Backend
    /// (DELETE /api/auth/account). Nach Erfolg wird der Benutzer abgemeldet.
    /// </summary>
    [RelayCommand]
    private async Task DeleteAccountAsync()
    {
        // Schritt 1: Bestaetigung einholen (verhindert versehentliches Loeschen)
        var confirmed = await _dialogs.Confirm(
            "Konto wirklich löschen?",
            "Ihr Profil, Ihre Inserate, Favoriten, Blockierungen und Benachrichtigungs-Einstellungen werden unwiderruflich gelöscht. Diese Aktion kann nicht rückgängig gemacht werden.");
        if (!confirmed)
        {
            return;
        }

        IsDeletingAccount = true;

        try
        {
            _logger.LogInformation("[UserProfile] Konto-Loeschung wird angefordert");

            // Schritt 2: Backend-Aufruf - der DeleteAccountHttpRequest wird automatisch
            // aus der OpenAPI-Spec generiert. Der Benutzer wird serverseitig per JWT erkannt.
            var response = await _mediator.Request(new DeleteAccountHttpRequest());

            if (response.Result?.Success == true)
            {
                _logger.LogInformation("[UserProfile] Konto erfolgreich geloescht");

                await _dialogs.Alert(
                    "Konto gelöscht",
                    "Ihr Konto und alle zugehörigen Daten wurden dauerhaft gelöscht.");

                // Schritt 3: Abmelden + Navigation zur Login-Seite
                await LogoutAsync();
            }
            else
            {
                await _dialogs.Alert(
                    "Löschung fehlgeschlagen",
                    "Ihr Konto konnte nicht gelöscht werden. Bitte versuchen Sie es später erneut.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserProfile] Konto-Loeschung fehlgeschlagen");
            await _dialogs.Alert(
                "Löschung fehlgeschlagen",
                "Ihr Konto konnte nicht gelöscht werden. Bitte prüfen Sie Ihre Internetverbindung und versuchen Sie es erneut.");
        }
        finally
        {
            IsDeletingAccount = false;
        }
    }

    private static string GetInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "?";

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return "?";

        if (parts.Length == 1)
            return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
