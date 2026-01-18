using Microsoft.Extensions.Logging;
#if __ANDROID__ || __IOS__ || __MACCATALYST__
using Shiny;
using Shiny.Push;
#endif

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

#if __ANDROID__ || __IOS__ || __MACCATALYST__
    /// <summary>
    /// Gets the current push notification status
    /// </summary>
    Task<AccessState> GetStatusAsync();
#endif
}

#if __ANDROID__ || __IOS__ || __MACCATALYST__
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class PushNotificationInitializer(
    IPushManager PushManager,
    ILogger<PushNotificationInitializer> Logger) : IPushNotificationInitializer
{
    /// <summary>
    /// Initializes push notifications and requests access
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation("Initializing push notifications...");

            // Request push notification permissions
            var result = await PushManager.RequestAccess(cancellationToken);

            switch (result.Status)
            {
                case AccessState.Available:
                    Logger.LogInformation("Push notifications enabled. Token: {Token}",
                        result.RegistrationToken);
                    break;

                case AccessState.Denied:
                    Logger.LogWarning("Push notification permission denied by user");
                    break;

                case AccessState.Disabled:
                    Logger.LogWarning("Push notifications are disabled on this device");
                    break;

                case AccessState.NotSetup:
                    Logger.LogWarning("Push notifications are not properly configured");
                    break;

                case AccessState.NotSupported:
                    Logger.LogWarning("Push notifications are not supported on this platform");
                    break;

                case AccessState.Restricted:
                    Logger.LogWarning("Push notifications are restricted (parental controls?)");
                    break;

                default:
                    Logger.LogWarning("Unknown push notification status: {Status}", result.Status);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize push notifications");
        }
    }

    /// <summary>
    /// Gets the current push notification status
    /// </summary>
    public async Task<AccessState> GetStatusAsync()
    {
        try
        {
            var result = await PushManager.RequestAccess();
            return result.Status;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get push notification status");
            return AccessState.Unknown;
        }
    }
}
#else
// Desktop/WebAssembly stub implementation - push notifications not supported
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class PushNotificationInitializer(ILogger<PushNotificationInitializer> Logger) : IPushNotificationInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Push notifications are not supported on this platform (Desktop/WebAssembly)");
        return Task.CompletedTask;
    }
}
#endif
