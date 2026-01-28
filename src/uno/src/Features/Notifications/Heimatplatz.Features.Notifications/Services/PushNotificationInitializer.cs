using Heimatplatz.Features.Notifications.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
#if __ANDROID__ || __IOS__ || __MACCATALYST__
using Shiny;
using Shiny.Push;
#endif

namespace Heimatplatz.Features.Notifications.Services;

#if __ANDROID__ || __IOS__ || __MACCATALYST__
public class PushNotificationInitializer(
    IPushManager pushManager,
    ILogger<PushNotificationInitializer> logger) : IPushNotificationInitializer
{
    /// <summary>
    /// Initializes push notifications after user login.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            logger.LogInformation("[PushNotificationInitializer] Initializing push notifications...");

            var result = await pushManager.RequestAccess();

            switch (result.Status)
            {
                case AccessState.Available:
                    logger.LogInformation("[PushNotificationInitializer] Push notifications enabled. Token: {Token}",
                        result.RegistrationToken);
                    break;

                case AccessState.Denied:
                    logger.LogWarning("[PushNotificationInitializer] Push notification permission denied by user");
                    break;

                case AccessState.Disabled:
                    logger.LogWarning("[PushNotificationInitializer] Push notifications are disabled on this device");
                    break;

                case AccessState.NotSetup:
                    logger.LogWarning("[PushNotificationInitializer] Push notifications are not properly configured");
                    break;

                case AccessState.NotSupported:
                    logger.LogWarning("[PushNotificationInitializer] Push notifications are not supported on this platform");
                    break;

                case AccessState.Restricted:
                    logger.LogWarning("[PushNotificationInitializer] Push notifications are restricted (parental controls?)");
                    break;

                default:
                    logger.LogWarning("[PushNotificationInitializer] Unknown push notification status: {Status}", result.Status);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[PushNotificationInitializer] Failed to initialize push notifications");
        }
    }

    /// <summary>
    /// Gets the current push notification status
    /// </summary>
    public async Task<AccessState> GetStatusAsync()
    {
        try
        {
            var result = await pushManager.RequestAccess();
            return result.Status;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[PushNotificationInitializer] Failed to get push notification status");
            return AccessState.Unknown;
        }
    }
}
#else
// Desktop/WebAssembly stub implementation - push notifications not supported
public class PushNotificationInitializer(ILogger<PushNotificationInitializer> logger) : IPushNotificationInitializer
{
    /// <summary>
    /// No-op on platforms that don't support push notifications
    /// </summary>
    public Task InitializeAsync()
    {
        logger.LogInformation("[PushNotificationInitializer] Push notifications are not supported on this platform (Desktop/WebAssembly)");
        return Task.CompletedTask;
    }
}
#endif
