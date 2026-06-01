using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Events;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Shiny.Mediator;
using UnoFramework.Contracts.Navigation;
using UnoFramework.Contracts.Pages;

namespace Heimatplatz.Features.Auth.Presentation;

/// <summary>
/// ViewModel fuer die Mein Profil Seite.
/// Zeigt alle verfuegbaren Benutzerdaten aus dem IAuthService an und bietet die
/// Moeglichkeit, das Konto endgueltig zu loeschen (Apple Guideline 5.1.1(v)).
/// </summary>
public partial class UserProfileViewModel : ObservableObject, IPageInfo, INavigationAware
{
    private readonly IAuthService _authService;
    private readonly IMediator _mediator;
    private readonly ILogger<UserProfileViewModel> _logger;

    [ObservableProperty]
    private string _userFullName = string.Empty;

    [ObservableProperty]
    private string _userInitials = string.Empty;

    [ObservableProperty]
    private string _userEmail = string.Empty;

    [ObservableProperty]
    private string _roleBadgeText = string.Empty;

    [ObservableProperty]
    private bool _isSeller;

    [ObservableProperty]
    private bool _isBuyer;

    /// <summary>True waehrend die Konto-Loeschung laeuft (zeigt Ladeindikator, blockiert UI).</summary>
    [ObservableProperty]
    private bool _isDeletingAccount;

    #region IPageInfo Implementation

    public PageType PageType => PageType.Detail;
    public string PageTitle => "Mein Profil";
    public Type? MainHeaderViewModel => null;

    #endregion

    public UserProfileViewModel(
        IAuthService authService,
        IMediator mediator,
        ILogger<UserProfileViewModel> logger)
    {
        _authService = authService;
        _mediator = mediator;
        _logger = logger;

        LoadUserData();
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
    /// Loescht das Konto des Benutzers nach einer ausdruecklichen Bestaetigung.
    /// Die eigentliche Loeschlogik liegt vollstaendig im Backend
    /// (DELETE /api/auth/account). Nach Erfolg wird der Benutzer abgemeldet.
    /// </summary>
    [RelayCommand]
    private async Task DeleteAccountAsync()
    {
        // Schritt 1: Bestaetigung einholen (verhindert versehentliches Loeschen)
        var confirmed = await ShowConfirmationDialogAsync();
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
            var response = await _mediator.Request(new Heimatplatz.Core.ApiClient.Generated.DeleteAccountHttpRequest());

            if (response.Result?.Success == true)
            {
                _logger.LogInformation("[UserProfile] Konto erfolgreich geloescht");

                await ShowInfoDialogAsync(
                    "Konto gelöscht",
                    "Ihr Konto und alle zugehörigen Daten wurden dauerhaft gelöscht.");

                // Schritt 3: Abmelden + Navigation zur Login-Seite (auf Shell-Ebene gehandled)
                await _mediator.Publish(new LogoutRequestedEvent());
            }
            else
            {
                await ShowInfoDialogAsync(
                    "Löschung fehlgeschlagen",
                    "Ihr Konto konnte nicht gelöscht werden. Bitte versuchen Sie es später erneut.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserProfile] Konto-Loeschung fehlgeschlagen");
            await ShowInfoDialogAsync(
                "Löschung fehlgeschlagen",
                "Ihr Konto konnte nicht gelöscht werden. Bitte prüfen Sie Ihre Internetverbindung und versuchen Sie es erneut.");
        }
        finally
        {
            IsDeletingAccount = false;
        }
    }

    private static async Task<bool> ShowConfirmationDialogAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "Konto wirklich löschen?",
            Content = "Ihr Profil, Ihre Inserate, Favoriten, Blockierungen und Benachrichtigungs-Einstellungen werden unwiderruflich gelöscht. Diese Aktion kann nicht rückgängig gemacht werden.",
            PrimaryButtonText = "Endgültig löschen",
            SecondaryButtonText = "Abbrechen",
            DefaultButton = ContentDialogButton.Secondary,
            XamlRoot = Window.Current?.Content?.XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private static async Task ShowInfoDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Window.Current?.Content?.XamlRoot
        };

        await dialog.ShowAsync();
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

    #region INavigationAware Implementation

    public void OnNavigatedTo(object? parameter)
    {
        _logger.LogDebug("[UserProfile] OnNavigatedTo");
        LoadUserData();
    }

    public void OnNavigatedFrom()
    {
        _logger.LogDebug("[UserProfile] OnNavigatedFrom");
    }

    #endregion
}
