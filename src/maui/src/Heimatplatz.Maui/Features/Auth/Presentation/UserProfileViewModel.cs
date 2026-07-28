using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Core.Theming;
using Heimatplatz.Maui.Events;
using Heimatplatz.Maui.Localization;
using Heimatplatz.Maui.Localization.Auth;
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
    private readonly IThemeService _themeService;
    // Dialog-Button-Texte (Ja/Nein/OK) - Shiny-Defaults sind englisch
    private readonly CommonStringsLocalized _commonLoc;
    private readonly ILogger<UserProfileViewModel> _logger;

    public UserProfileStringsLocalized Loc { get; }

    [ObservableProperty]
    public partial string UserFullName { get; set; }

    [ObservableProperty]
    public partial string UserInitials { get; set; }

    [ObservableProperty]
    public partial string UserEmail { get; set; }

    [ObservableProperty]
    public partial string RoleBadgeText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBuyerOnly))]
    public partial bool IsSeller { get; set; }

    /// <summary>True fuer reine Kaeufer-Konten (steuert die 2er- statt 3er-Kachelzeile).</summary>
    public bool IsBuyerOnly => !IsSeller;

    // ===== Konto-Ueberblick (Zaehler-Kacheln) =====

    [ObservableProperty]
    public partial string FavoritesCountText { get; set; }

    [ObservableProperty]
    public partial string BlockedCountText { get; set; }

    [ObservableProperty]
    public partial string MyPropertiesCountText { get; set; }

    /// <summary>Gruenes "E-Mail bestaetigt"-Badge im Hero - erst nach erfolgreichem Profil-Load sichtbar.</summary>
    [ObservableProperty]
    public partial bool ShowVerifiedBadge { get; set; }

    // ===== Design-Umschalter (Hero rechts oben, wie der Web-Header) =====

    [ObservableProperty]
    public partial string ThemeIcon { get; set; }

    [ObservableProperty]
    public partial string ThemeModeLabel { get; set; }

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

    /// <summary>Telefonnummer (optional) - erscheint als Erreichbarkeit in eigenen Inseraten</summary>
    [ObservableProperty]
    public partial string EditPhone { get; set; }

    /// <summary>True wenn ein Firmenname noetig ist (Makler oder Verwaltung)</summary>
    public bool EditNeedsCompanyName => EditIsBroker || EditIsPropertyManager;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProfileStatus))]
    public partial string? ProfileStatusMessage { get; set; }

    public bool HasProfileStatus => !string.IsNullOrEmpty(ProfileStatusMessage);

    /// <summary>Fehler rot darstellen - Erfolg und Fehler sahen sonst identisch aus</summary>
    [ObservableProperty]
    public partial bool IsProfileStatusError { get; set; }

    [ObservableProperty]
    public partial bool IsSavingProfile { get; set; }

    // ===== E-Mail-Bestaetigung =====

    /// <summary>Startet mit true, damit die "nicht bestaetigt"-UI nicht kurz aufblitzt, bis das Profil geladen ist.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmailNotVerified))]
    public partial bool IsEmailVerified { get; set; }

    public bool IsEmailNotVerified => !IsEmailVerified;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVerificationStatus))]
    public partial string? VerificationStatusMessage { get; set; }

    public bool HasVerificationStatus => !string.IsNullOrEmpty(VerificationStatusMessage);

    [ObservableProperty]
    public partial bool IsResendingVerification { get; set; }

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

    /// <summary>Fehler rot darstellen - Erfolg und Fehler sahen sonst identisch aus</summary>
    [ObservableProperty]
    public partial bool IsPasswordStatusError { get; set; }

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
        IThemeService themeService,
        ILogger<UserProfileViewModel> logger,
        UserProfileStringsLocalized loc,
        CommonStringsLocalized commonLoc)
    {
        _authService = authService;
        _mediator = mediator;
        _navigator = navigator;
        _dialogs = dialogs;
        _themeService = themeService;
        _logger = logger;
        Loc = loc;
        _commonLoc = commonLoc;

        UserFullName = string.Empty;
        UserInitials = string.Empty;
        UserEmail = string.Empty;
        RoleBadgeText = string.Empty;
        SellerTypeLabel = string.Empty;
        EditFirstName = string.Empty;
        EditLastName = string.Empty;
        EditIsPrivate = true;
        EditCompanyName = string.Empty;
        EditPhone = string.Empty;
        IsEmailVerified = true;
        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
        NewPasswordConfirm = string.Empty;
        FavoritesCountText = "–";
        BlockedCountText = "–";
        MyPropertiesCountText = "–";
        ThemeIcon = string.Empty;
        ThemeModeLabel = string.Empty;
        UpdateThemeDisplay();

        LoadUserData();
    }

    /// <summary>
    /// Die Seite blendet daraufhin kurz den Modusnamen ein. Noetig, weil ein
    /// Wechsel nicht immer sichtbar ist: System -&gt; Hell aendert bei hellem
    /// System-Theme nur das Symbol - der Nutzer tippte sonst ins Leere.
    /// Bewusst ein Ereignis (kein Zustand): nur ein echter Tipp zeigt die
    /// Einblendung, das Wiederherstellen beim Seitenaufbau nicht.
    /// </summary>
    public event EventHandler? ThemeModeToastRequested;

    /// <summary>
    /// Schaltet den Design-Modus im Zyklus System -&gt; Hell -&gt; Dunkel weiter.
    /// Die Seiten aktualisieren sich ueber ihre AppThemeBindings von selbst.
    /// </summary>
    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.CycleMode();
        UpdateThemeDisplay();
        ThemeModeToastRequested?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateThemeDisplay()
    {
        (ThemeIcon, ThemeModeLabel) = _themeService.Mode switch
        {
            AppThemeMode.Light => ("icon_theme_light.png", Loc.ThemeLight),
            AppThemeMode.Dark => ("icon_theme_dark.png", Loc.ThemeDark),
            _ => ("icon_theme_system.png", Loc.ThemeSystem)
        };
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
        _ = LoadStatsAsync();
    }

    /// <summary>
    /// Laedt die Zaehler fuer die Ueberblicks-Kacheln (Favoriten/Blockiert/eigene
    /// Inserate). PageSize 1, weil nur der Total-Wert gebraucht wird; Fehler lassen
    /// den Platzhalter stehen - die Kacheln sind Komfort, keine Pflichtdaten.
    /// </summary>
    private async Task LoadStatsAsync()
    {
        try
        {
            var favoritesTask = _mediator.Request(new GetUserFavoritesHttpRequest { Page = 1, PageSize = 1 });
            var blockedTask = _mediator.Request(new GetUserBlockedHttpRequest { Page = 1, PageSize = 1 });

            var (_, favorites) = await favoritesTask;
            if (favorites != null)
                FavoritesCountText = favorites.Total.ToString();

            var (_, blocked) = await blockedTask;
            if (blocked != null)
                BlockedCountText = blocked.Total.ToString();

            if (IsSeller)
            {
                var (_, mine) = await _mediator.Request(new GetUserPropertiesHttpRequest { Page = 1, PageSize = 1 });
                if (mine != null)
                    MyPropertiesCountText = mine.Total.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserProfile] Konto-Zaehler konnten nicht geladen werden");
        }
    }

    // ===== Schnellzugriff-Navigation =====

    [RelayCommand]
    private Task GoToFavoritesAsync() => _navigator.NavigateTo("Favorites", relativeNavigation: false);

    [RelayCommand]
    private Task GoToBlockedAsync() => _navigator.NavigateTo("Blocked", relativeNavigation: false);

    [RelayCommand]
    private Task GoToMyPropertiesAsync() => _navigator.NavigateTo("MyProperties", relativeNavigation: false);

    [RelayCommand]
    private Task GoToAddPropertyAsync() => _navigator.NavigateTo("PropertyWizard");

    [RelayCommand]
    private Task GoToNotificationSettingsAsync() => _navigator.NavigateTo("NotificationSettings", relativeNavigation: false);

    [RelayCommand]
    private Task GoToFilterSettingsAsync() => _navigator.NavigateTo("FilterSettings", relativeNavigation: false);

    [RelayCommand]
    private Task GoToForgotPasswordAsync() => _navigator.NavigateTo("ForgotPassword");

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
            EditPhone = profile.Phone ?? string.Empty;
            IsEmailVerified = profile.EmailVerified;
            ShowVerifiedBadge = profile.EmailVerified;
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
        IsProfileStatusError = true;

        if (string.IsNullOrWhiteSpace(EditFirstName) || string.IsNullOrWhiteSpace(EditLastName))
        {
            ProfileStatusMessage = Loc.ValidationNamesRequired;
            return;
        }

        if (EditWantsToSell && EditNeedsCompanyName && string.IsNullOrWhiteSpace(EditCompanyName))
        {
            ProfileStatusMessage = Loc.ValidationCompanyNameRequired;
            return;
        }

        if (!IsValidPhoneFormat(EditPhone))
        {
            ProfileStatusMessage = Loc.ValidationPhoneInvalid;
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
                    CompanyName = EditWantsToSell && EditNeedsCompanyName ? EditCompanyName : null,
                    Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone.Trim()
                }
            });

            if (result == null)
            {
                ProfileStatusMessage = Loc.ProfileSaveFailedRetry;
                return;
            }

            // Neuer Access Token traegt die aktualisierten Claims sofort
            _authService.UpdateAccessToken(result.AccessToken);
            LoadUserData();
            // Kachelzeile passt sich der neuen Rolle an (z.B. Inserate-Zaehler fuer neue Verkaeufer)
            _ = LoadStatsAsync();
            IsProfileStatusError = false;
            ProfileStatusMessage = Loc.ProfileSaved;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserProfile] Profil-Update fehlgeschlagen");
            ProfileStatusMessage = Loc.ProfileSaveFailedCheckInput;
        }
        finally
        {
            IsSavingProfile = false;
        }
    }

    /// <summary>
    /// Fordert den Neuversand der Verifikations-Mail an (nur sichtbar solange die
    /// E-Mail-Adresse nicht bestaetigt ist). Bestaetigt wird ueber den Link in der Mail.
    /// </summary>
    [RelayCommand]
    private async Task ResendVerificationEmailAsync()
    {
        VerificationStatusMessage = null;
        IsResendingVerification = true;
        try
        {
            var (_, result) = await _mediator.Request(new ResendVerificationEmailHttpRequest
            {
                Body = new ResendVerificationEmailRequest()
            });

            if (result?.AlreadyVerified == true)
            {
                IsEmailVerified = true;
                ShowVerifiedBadge = true;
                VerificationStatusMessage = Loc.VerificationAlreadyVerified;
            }
            else
            {
                VerificationStatusMessage = Loc.VerificationEmailSent;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserProfile] Verifikations-Mail-Versand fehlgeschlagen");
            VerificationStatusMessage = Loc.VerificationSendFailed;
        }
        finally
        {
            IsResendingVerification = false;
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
        IsPasswordStatusError = true;

        if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword))
        {
            PasswordStatusMessage = Loc.ValidationPasswordsRequired;
            return;
        }

        if (NewPassword.Length < 8)
        {
            PasswordStatusMessage = Loc.ValidationNewPasswordTooShort;
            return;
        }

        if (NewPassword != NewPasswordConfirm)
        {
            PasswordStatusMessage = Loc.ValidationPasswordMismatch;
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
                PasswordStatusMessage = Loc.PasswordChangeFailed;
                return;
            }

            _authService.UpdateTokens(result.AccessToken, result.RefreshToken, result.ExpiresAt);

            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            NewPasswordConfirm = string.Empty;
            IsPasswordStatusError = false;
            PasswordStatusMessage = Loc.PasswordChanged;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserProfile] Passwort-Aenderung fehlgeschlagen");
            PasswordStatusMessage = Loc.PasswordChangeFailedCheckCurrent;
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
            "Broker" => Loc.SellerTypeBroker,
            "PropertyManager" => Loc.SellerTypePropertyManager,
            "Private" => Loc.SellerTypePrivate,
            _ => string.Empty
        };

        // Jeder Account ist Kaeufer; Verkaeufer wird mit Anbietertyp ausgewiesen
        RoleBadgeText = IsSeller && SellerTypeLabel.Length > 0
            ? Loc.RoleBadgeSellerFormat(SellerTypeLabel)
            : Loc.RoleBadgeBuyer;

        _logger.LogInformation("[UserProfile] Benutzerdaten geladen: {Name}, {Email}", UserFullName, UserEmail);
    }

    /// <summary>
    /// Meldet den Benutzer ab. Publiziert nur das LogoutRequestedEvent - Cache-Flush,
    /// ClearAuthentication und Navigation macht zentral der AppStartupService-Handler.
    /// ClearAuthentication darf hier NICHT vorab laufen: Der Handler leitet den
    /// Cache-Scope aus authService.UserId ab - waere die schon null, wuerde der
    /// anonyme statt der benutzerbezogene Offline-Cache geloescht (Favoriten,
    /// eigene Inserate und Filter blieben auf geteilten Geraeten liegen).
    /// </summary>
    [RelayCommand]
    private async Task LogoutAsync()
    {
        _logger.LogInformation("[UserProfile] Logout angefordert");

        await _mediator.Publish(new LogoutRequestedEvent());
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
            Loc.DeleteConfirmTitle,
            Loc.DeleteConfirmText,
            _commonLoc.Yes,
            _commonLoc.No);
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
                    Loc.DeleteSuccessTitle,
                    Loc.DeleteSuccessText,
                    _commonLoc.Ok);

                // Schritt 3: Abmelden + Navigation zur Login-Seite
                await LogoutAsync();
            }
            else
            {
                await _dialogs.Alert(
                    Loc.DeleteFailedTitle,
                    Loc.DeleteFailedText,
                    _commonLoc.Ok);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserProfile] Konto-Loeschung fehlgeschlagen");
            await _dialogs.Alert(
                Loc.DeleteFailedTitle,
                Loc.DeleteFailedNetworkText,
                _commonLoc.Ok);
        }
        finally
        {
            IsDeletingAccount = false;
        }
    }

    /// <summary>
    /// Spiegelt UserInputValidator.NormalizePhone (API) fuer sofortiges UX-Feedback;
    /// massgeblich bleibt ausschliesslich die serverseitige Validierung.
    /// </summary>
    private static bool IsValidPhoneFormat(string? phone)
    {
        var trimmed = phone?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return true;

        const int minPhoneDigits = 4;
        const int maxPhoneLength = 50;
        var digitCount = trimmed.Count(char.IsAsciiDigit);
        var onlyValidChars = trimmed.All(c => char.IsAsciiDigit(c) || c is ' ' or '+' or '-' or '/' or '(' or ')' or '.');

        return trimmed.Length <= maxPhoneLength && digitCount >= minPhoneDigits && onlyValidChars;
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
