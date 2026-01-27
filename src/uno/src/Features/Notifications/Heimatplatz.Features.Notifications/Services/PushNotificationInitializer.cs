using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
#if __ANDROID__ || __IOS__ || __MACCATALYST__
using Shiny;
using Shiny.Push;
#endif

namespace Heimatplatz.Features.Notifications.Services;

/// <summary>
/// Initializes push notifications and requests permissions.
/// Listens to AuthenticationStateChanged and only registers when a user logs in.
/// </summary>
public interface IPushNotificationInitializer
{
#if __ANDROID__ || __IOS__ || __MACCATALYST__
    /// <summary>
    /// Gets the current push notification status
    /// </summary>
    Task<AccessState> GetStatusAsync();
#endif
}

#if __ANDROID__ || __IOS__ || __MACCATALYST__
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class PushNotificationInitializer : IPushNotificationInitializer
{
    private readonly IPushManager _pushManager;
    private readonly ILogger<PushNotificationInitializer> _logger;

    public PushNotificationInitializer(
        IPushManager pushManager,
        IAuthService authService,
        ILogger<PushNotificationInitializer> logger)
    {
        _pushManager = pushManager;
        _logger = logger;

        authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    private async void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        if (!isAuthenticated)
            return;

        try
        {
            _logger.LogInformation("User logged in — initializing push notifications...");

            var result = await _pushManager.RequestAccess();

            switch (result.Status)
            {
                case AccessState.Available:
                    _logger.LogInformation("Push notifications enabled. Token: {Token}",
                        result.RegistrationToken);
                    break;

                case AccessState.Denied:
                    _logger.LogWarning("Push notification permission denied by user");
                    break;

                case AccessState.Disabled:
                    _logger.LogWarning("Push notifications are disabled on this device");
                    break;

                case AccessState.NotSetup:
                    _logger.LogWarning("Push notifications are not properly configured");
                    break;

                case AccessState.NotSupported:
                    _logger.LogWarning("Push notifications are not supported on this platform");
                    break;

                case AccessState.Restricted:
                    _logger.LogWarning("Push notifications are restricted (parental controls?)");
                    break;

                default:
                    _logger.LogWarning("Unknown push notification status: {Status}", result.Status);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize push notifications after login");
        }
    }

    /// <summary>
    /// Gets the current push notification status
    /// </summary>
    public async Task<AccessState> GetStatusAsync()
    {
        try
        {
            var result = await _pushManager.RequestAccess();
            return result.Status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get push notification status");
            return AccessState.Unknown;
        }
    }
}
#else
// Desktop/WebAssembly stub implementation - push notifications not supported
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class PushNotificationInitializer(ILogger<PushNotificationInitializer> logger) : IPushNotificationInitializer
{
    // Log once at construction to indicate push is not supported on this platform
    private readonly ILogger<PushNotificationInitializer> _logger = logger;

    public void LogPlatformNotSupported()
    {
        _logger.LogInformation("Push notifications are not supported on this platform (Desktop/WebAssembly)");
    }
}
#endif
