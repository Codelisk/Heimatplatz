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
[ShellMap<UserProfilePage>("UserProfile", registerRoute: false)]
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

    /// <summary>Beschreibung des Anbietertyps (Privatperson/Makler/Hausverwaltung)</summary>
    [ObservableProperty]
    public partial string SellerTypeLabel { get; set; }

    /// <summary>True waehrend die Konto-Loeschung laeuft (zeigt Ladeindikator, blockiert UI).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotDeletingAccount))]
    public partial bool IsDeletingAccount { get; set; }

    /// <summary>Sperrt z.B. den Abmelden-Button waehrend der Konto-Loeschung.</summary>
    public bool IsNotDeletingAccount => !IsDeletingAccount;

    // ===== Profil bearbeiten (Verkaeufer werden / Anbietertyp wechseln) =====

    [ObservableProperty]
    public partial string EditFirstName { get; set; }

    [ObservableProperty]
    public partial string EditLastName { get; set; }

    [ObservableProperty]
    public partial bool EditWantsToSell { get; set; }

    [ObservableProperty]
    public partial bool EditIsPrivate { get; set; }

    [ObservableProperty]
    public partial bool EditIsBroker { get; set; }

    [ObservableProperty]
    public partial bool EditIsPropertyManager { get; set; }

    [ObservableProperty]
    public partial string EditCompanyName { get; set; }

    /// <summary>True wenn ein Firmenname noetig ist (Makler oder Verwaltung)</summary>
    public bool EditNeedsCompanyName => EditIsBroker || EditIsPropertyManager;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProfileStatus))]
    public partial string? ProfileStatusMessage { get; set; }

    public bool HasProfileStatus => !string.IsNullOrEmpty(ProfileStatusMessage);

    [ObservableProperty]
    public partial bool IsSavingProfile { get; set; }

    // ===== Passwort aendern =====

    [ObservableProperty]
    public partial string CurrentPassword { get; set; }

    [ObservableProperty]
    public partial string NewPassword { get; set; }

    [ObservableProperty]
    public partial string NewPasswordConfirm { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPasswordStatus))]
    public partial string? PasswordStatusMessage { get; set; }

    public bool HasPasswordStatus => !string.IsNullOrEmpty(PasswordStatusMessage);

    [ObservableProperty]
    public partial bool IsChangingPassword { get; set; }

    partial void OnEditWantsToSellChanged(bool value)
    {
        if (!value)
        {
            EditIsPrivate = true;
            EditIsBroker = false;
            EditIsPropertyManager = false;
            EditCompanyName = string.Empty;
        }
    }
    partial void OnEditIsPrivateChanged(bool value)
    {
        if (value)
        {
            EditIsBroker = false;
            EditIsPropertyManager = false;
        }
        OnPropertyChanged(nameof(EditNeedsCompanyName));
    }
    partial void OnEditIsBrokerChanged(bool value)
    {
        if (value)
        {
            EditIsPrivate = false;
            EditIsPropertyManager = false;
        }
        OnPropertyChanged(nameof(EditNeedsCompanyName));
    }
    partial void OnEditIsPropertyManagerChanged(bool value)
    {
        if (value)
        {
            EditIsPrivate = false;
            EditIsBroker = false;
        }
        OnPropertyChanged(nameof(EditNeedsCompanyName));
    }

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
        SellerTypeLabel = string.Empty;
        EditFirstName = string.Empty;
        EditLastName = string.Empty;
        EditIsPrivate = true;
        EditCompanyName = string.Empty;
        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
        NewPasswordConfirm = string.Empty;

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
        _ = LoadProfileForEditingAsync();
    }

    /// <summary>
    /// Laedt das Profil vom Server und fuellt die Bearbeitungsfelder
    /// (Verkaeufer-Einstellungen sind nachtraeglich aenderbar).
    /// </summary>
    private async Task LoadProfileForEditingAsync()
    {
        try
        {
            var (_, profile) = await _mediator.Request(new GetProfileHttpRequest());
            if (profile == null)
                return;

            EditFirstName = profile.FirstName;
            EditLastName = profile.LastName;
            EditWantsToSell = profile.SellerType != null;
            EditIsPrivate = profile.SellerType is null or ApiClient.Generated.SellerType.Private;
            EditIsBroker = profile.SellerType == ApiClient.Generated.SellerType.Broker;
            EditIsPropertyManager = profile.SellerType == ApiClient.Generated.SellerType.PropertyManager;
            EditCompanyName = profile.CompanyName ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserProfile] Profil konnte nicht geladen werden");
        }
    }

    /// <summary>
    /// Speichert Namen und Verkaeufer-Einstellungen. Der Server liefert einen frischen
    /// Access Token mit aktualisierten Claims (Seller/SellerType) zurueck.
    /// </summary>
    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        ProfileStatusMessage = null;

        if (string.IsNullOrWhiteSpace(EditFirstName) || string.IsNullOrWhiteSpace(EditLastName))
        {
            ProfileStatusMessage = "Bitte Vor- und Nachnamen angeben.";
            return;
        }

        if (EditWantsToSell && EditNeedsCompanyName && string.IsNullOrWhiteSpace(EditCompanyName))
        {
            ProfileStatusMessage = "Bitte den Firmennamen angeben.";
            return;
        }

        ApiClient.Generated.SellerType? sellerType = null;
        if (EditWantsToSell)
        {
            sellerType = EditIsBroker
                ? ApiClient.Generated.SellerType.Broker
                : EditIsPropertyManager
                    ? ApiClient.Generated.SellerType.PropertyManager
                    : ApiClient.Generated.SellerType.Private;
        }

        IsSavingProfile = true;
        try
        {
            var (_, result) = await _mediator.Request(new UpdateProfileHttpRequest
            {
                Body = new UpdateProfileRequest
                {
                    FirstName = EditFirstName,
                    LastName = EditLastName,
                    SellerType = sellerType,
                    CompanyName = EditWantsToSell && EditNeedsCompanyName ? EditCompanyName : null
                }
            });

            if (result == null)
            {
                ProfileStatusMessage = "Speichern fehlgeschlagen. Bitte versuchen Sie es erneut.";
                return;
            }

            // Neuer Access Token traegt die aktualisierten Claims sofort
            _authService.UpdateAccessToken(result.AccessToken);
            LoadUserData();
            ProfileStatusMessage = "Profil gespeichert.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserProfile] Profil-Update fehlgeschlagen");
            ProfileStatusMessage = "Speichern fehlgeschlagen. Bitte überprüfen Sie Ihre Eingaben.";
        }
        finally
        {
            IsSavingProfile = false;
        }
    }

    /// <summary>
    /// Aendert das Passwort. Der Server widerruft alle anderen Sitzungen und liefert
    /// ein frisches Token-Paar fuer dieses Geraet.
    /// </summary>
    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        PasswordStatusMessage = null;

        if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword))
        {
            PasswordStatusMessage = "Bitte aktuelles und neues Passwort angeben.";
            return;
        }

        if (NewPassword.Length < 8)
        {
            PasswordStatusMessage = "Das neue Passwort muss mindestens 8 Zeichen lang sein.";
            return;
        }

        if (NewPassword != NewPasswordConfirm)
        {
            PasswordStatusMessage = "Die Passwörter stimmen nicht überein.";
            return;
        }

        IsChangingPassword = true;
        try
        {
            var (_, result) = await _mediator.Request(new ChangePasswordHttpRequest
            {
                Body = new ChangePasswordRequest
                {
                    CurrentPassword = CurrentPassword,
                    NewPassword = NewPassword
                }
            });

            if (result == null)
            {
                PasswordStatusMessage = "Passwort-Änderung fehlgeschlagen.";
                return;
            }

            _authService.UpdateTokens(result.AccessToken, result.RefreshToken, result.ExpiresAt);

            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            NewPasswordConfirm = string.Empty;
            PasswordStatusMessage = "Passwort geändert. Andere Geräte wurden abgemeldet.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserProfile] Passwort-Aenderung fehlgeschlagen");
            PasswordStatusMessage = "Passwort-Änderung fehlgeschlagen. Ist das aktuelle Passwort korrekt?";
        }
        finally
        {
            IsChangingPassword = false;
        }
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

        SellerTypeLabel = _authService.SellerType switch
        {
            "Broker" => "Makler / Agentur",
            "PropertyManager" => "Hausverwaltung",
            "Private" => "Privatperson",
            _ => string.Empty
        };

        // Jeder Account ist Kaeufer; Verkaeufer wird mit Anbietertyp ausgewiesen
        RoleBadgeText = IsSeller && SellerTypeLabel.Length > 0
            ? $"Verkäufer · {SellerTypeLabel}"
            : "Käufer";

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
