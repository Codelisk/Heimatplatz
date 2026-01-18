#if __ANDROID__ || __IOS__ || __MACCATALYST__
using Microsoft.Extensions.Logging;
using Shiny.Push;

namespace Heimatplatz.Features.Notifications.Services;

/// <summary>
/// Handles push notification events from Shiny.Push (mobile platforms only)
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class PushNotificationDelegate(
    ILogger<PushNotificationDelegate> Logger,
    INotificationService NotificationService) : IPushDelegate
{
    /// <summary>
    /// Called when user taps on a push notification
    /// </summary>
    public async Task OnEntry(PushNotificationResponse response)
    {
        Logger.LogInformation("Push notification tapped: {Title}", response.Notification?.Title);

        // TODO: Navigate to property detail page if notification contains propertyId
        if (response.Notification?.Data?.ContainsKey("propertyId") == true)
        {
            var propertyId = response.Notification.Data["propertyId"];
            Logger.LogInformation("Navigate to property: {PropertyId}", propertyId);
            // Navigation will be implemented in next step
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Called when a push notification is received while app is in foreground
    /// </summary>
    public async Task OnReceived(PushNotification notification)
    {
        Logger.LogInformation("Push notification received: {Title} - {Message}",
            notification.Title, notification.Message);

        // Show local notification if app is in foreground
        // This allows users to see the notification even when the app is active
        await Task.CompletedTask;
    }

    /// <summary>
    /// Called when the device token changes
    /// </summary>
    public async Task OnNewToken(string token)
    {
        Logger.LogInformation("New push token received: {Token}", token);

        // Register the new token with our API
        try
        {
            var platform = GetCurrentPlatform();
            var success = await NotificationService.RegisterDeviceAsync(token, platform, CancellationToken.None);

            if (success)
            {
                Logger.LogInformation("Device token registered successfully with API");
            }
            else
            {
                Logger.LogWarning("Failed to register device token with API");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error registering device token");
        }
    }

    /// <summary>
    /// Called when push notifications are unregistered
    /// </summary>
    public Task OnUnRegistered()
    {
        Logger.LogInformation("Push notifications unregistered");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current platform identifier
    /// </summary>
    private static string GetCurrentPlatform()
    {
#if __ANDROID__
        return "Android";
#elif __IOS__
        return "iOS";
#elif __MACCATALYST__
        return "MacCatalyst";
#else
        return "Unknown";
#endif
    }
}
#endif
