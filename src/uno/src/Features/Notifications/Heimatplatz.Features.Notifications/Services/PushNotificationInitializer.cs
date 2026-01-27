using Microsoft.Extensions.Logging;

namespace Heimatplatz.Features.Notifications.Services;

/// <summary>
/// Initializes push notifications and requests permissions
/// </summary>
public interface IPushNotificationInitializer
{
    /// <summary>
    /// Initializes push notifications and requests access
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current push notification status
    /// </summary>
    Task<PushAccessState> GetStatusAsync();
}

/// <summary>
/// Push notification initializer using Firebase (Android) or native providers (iOS)
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class PushNotificationInitializer(
    IFirebasePushManager pushManager,
    ILogger<PushNotificationInitializer> logger) : IPushNotificationInitializer
{
    /// <summary>
    /// Initializes push notifications and requests access
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Initializing push notifications...");

            // Request push notification permissions
            var result = await pushManager.RequestAccessAsync(cancellationToken);

            switch (result.Status)
            {
                case PushAccessState.Available:
                    logger.LogInformation("Push notifications enabled. Token: {Token}",
                        result.RegistrationToken);
                    break;

                case PushAccessState.Denied:
                    logger.LogWarning("Push notification permission denied by user");
                    break;

                case PushAccessState.Disabled:
                    logger.LogWarning("Push notifications are disabled on this device");
                    break;

                case PushAccessState.NotSupported:
                    logger.LogWarning("Push notifications are not supported on this platform");
                    break;

                case PushAccessState.Restricted:
                    logger.LogWarning("Push notifications are restricted (parental controls?)");
                    break;

                default:
                    logger.LogWarning("Unknown push notification status: {Status}", result.Status);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize push notifications");
        }
    }

    /// <summary>
    /// Gets the current push notification status
    /// </summary>
    public async Task<PushAccessState> GetStatusAsync()
    {
        try
        {
            var result = await pushManager.RequestAccessAsync();
            return result.Status;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get push notification status");
            return PushAccessState.Unknown;
        }
    }
}
