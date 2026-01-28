#if __ANDROID__ || __IOS__ || __MACCATALYST__
using Microsoft.Extensions.Logging;
using Shiny.Notifications;
using Shiny.Push;

namespace Heimatplatz.Features.Notifications.Services;

/// <summary>
/// Handles push notification events from Shiny.Push (mobile platforms only)
/// Registered via AddPush&lt;PushNotificationDelegate&gt;() - no [Service] attribute needed
/// </summary>
public class PushNotificationDelegate(
    ILogger<PushNotificationDelegate> Logger,
    INotificationService NotificationService,
    INotificationManager ShinyNotificationManager) : IPushDelegate
{
    /// <summary>
    /// Called when user taps on a push notification
    /// </summary>
    public async Task OnEntry(PushNotification notification)
    {
        var (title, message) = ExtractNotificationContent(notification);
        Logger.LogInformation("Push notification tapped: {Title}", title);

        // TODO: Navigate to property detail page if notification contains propertyId
        var propertyId = GetDataValue(notification.Data, "propertyId");
        if (propertyId is not null)
        {
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
        var (title, message) = ExtractNotificationContent(notification);
        Logger.LogInformation("Push notification received: {Title} - {Message}", title, message);

        // Show local notification when app is in foreground
        // Android doesn't auto-display notifications when app is active
        if (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(message))
        {
            await ShowLocalNotificationAsync(title, message);
        }
    }

    /// <summary>
    /// Extracts title and message from push notification.
    /// Reads from Notification object first (FCM notification payload),
    /// then falls back to Data dictionary (FCM data payload).
    /// </summary>
    private static (string title, string message) ExtractNotificationContent(PushNotification notification)
    {
        // Try Notification object first (standard FCM notification)
        var title = notification.Notification?.Title;
        var message = notification.Notification?.Message;

        // Fall back to Data dictionary if Notification is empty
        if (string.IsNullOrEmpty(title))
            title = GetDataValue(notification.Data, "title");
        if (string.IsNullOrEmpty(message))
            message = GetDataValue(notification.Data, "body") ?? GetDataValue(notification.Data, "message");

        return (title ?? "Notification", message ?? string.Empty);
    }

    private static string? GetDataValue(IDictionary<string, string>? data, string key)
        => data is not null && data.TryGetValue(key, out var value) ? value : null;

    /// <summary>
    /// Shows a local notification when the app is in foreground using Shiny.Notifications
    /// </summary>
    private async Task ShowLocalNotificationAsync(string title, string message)
    {
        try
        {
            // Use Shiny.Notifications to show local notification
            var notification = new Shiny.Notifications.Notification
            {
                Title = title,
                Message = message,
                Channel = Shiny.Notifications.Channel.Default.Identifier
            };

            await ShinyNotificationManager.Send(notification);
            Logger.LogInformation("Local notification shown via Shiny: {Title}", title);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to show local notification");
        }
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
    public Task OnUnRegistered(string token)
    {
        Logger.LogInformation("Push notifications unregistered for token: {Token}", token);
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
