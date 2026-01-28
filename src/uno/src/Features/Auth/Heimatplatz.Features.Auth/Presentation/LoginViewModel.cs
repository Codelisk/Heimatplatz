using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Core.ApiClient.Generated;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;
using Uno.Extensions.Navigation;
#if __ANDROID__ || __IOS__ || __MACCATALYST__
using Shiny;
using Shiny.Push;
#endif

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
    private readonly IServiceProvider _serviceProvider;

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
        ILogger<LoginViewModel> logger,
        IServiceProvider serviceProvider)
    {
        _mediator = mediator;
        _authService = authService;
        _navigator = navigator;
        _logger = logger;
        _serviceProvider = serviceProvider;
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

            // Push Notifications initialisieren (mobile platforms only)
#if __ANDROID__ || __IOS__ || __MACCATALYST__
            await InitializePushNotificationsAsync();
#endif

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

#if __ANDROID__ || __IOS__ || __MACCATALYST__
    private async Task InitializePushNotificationsAsync()
    {
        try
        {
            _logger.LogInformation("[LoginViewModel] Attempting to resolve IPushManager...");

            // Resolve IPushManager lazily to avoid DI issues during ViewModel construction
            var pushManager = _serviceProvider.GetService<IPushManager>();
            if (pushManager == null)
            {
                _logger.LogWarning("[LoginViewModel] PushManager not available - push notifications disabled");
                return;
            }

            _logger.LogInformation("[LoginViewModel] IPushManager resolved successfully, requesting access...");

            // Request push access - wrapped in try-catch to prevent app crash
            try
            {
                var accessResult = await pushManager.RequestAccess().ConfigureAwait(false);

                switch (accessResult.Status)
                {
                    case AccessState.Available:
                        _logger.LogInformation("[LoginViewModel] Push notifications enabled. Token: {Token}",
                            accessResult.RegistrationToken);
                        break;

                    case AccessState.Denied:
                        _logger.LogWarning("[LoginViewModel] Push notification permission denied by user");
                        break;

                    default:
                        _logger.LogWarning("[LoginViewModel] Push notification status: {Status}", accessResult.Status);
                        break;
                }
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "[LoginViewModel] Push RequestAccess failed - this is non-fatal");
            }
        }
        catch (Exception ex)
        {
            // Don't fail login if push notifications fail
            _logger.LogError(ex, "[LoginViewModel] Failed to initialize push notifications");
        }
    }
#endif
}
