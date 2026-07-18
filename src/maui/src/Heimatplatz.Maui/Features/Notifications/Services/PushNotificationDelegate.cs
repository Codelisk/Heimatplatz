#if ANDROID || IOS
using Heimatplatz.Maui.Core.DeepLink;
using Heimatplatz.Maui.Localization.Notifications;
using Microsoft.Extensions.Logging;
using Shiny.Notifications;
using Shiny.Push;
#if IOS
using UIKit;
using UserNotifications;
#endif

namespace Heimatplatz.Maui.Features.Notifications.Services;

/// <summary>
/// Handles push notification events from Shiny.Push (mobile platforms only).
/// Registered via AddPush&lt;PushNotificationDelegate&gt;() - no [Singleton] attribute needed.
/// On iOS, IApplePushDelegate controls the foreground presentation (Banner | Sound | List) -
/// this replaces the UNUserNotificationCenterDelegate of the Uno app.
/// </summary>
public class PushNotificationDelegate(
    ILogger<PushNotificationDelegate> logger,
    INotificationService notificationService,
    IDeepLinkService deepLinkService,
    INotificationManager shinyNotificationManager,
    NotificationStringsLocalized loc) :
#if IOS
    IApplePushDelegate
#else
    IPushDelegate
#endif
{
    /// <summary>
    /// Called when user taps on a push notification.
    /// Navigates via the DeepLink pipeline if the notification carries deep-link data.
    /// </summary>
    public async Task OnEntry(PushNotification notification)
    {
        var (title, _) = ExtractNotificationContent(notification);
        logger.LogInformation("Push notification tapped: {Title}", title);

        var deepLinkUri = ResolveDeepLinkUri(notification.Data);
        if (deepLinkUri is null)
            return;

        if (!deepLinkService.CanHandleUri(deepLinkUri))
        {
            logger.LogWarning("Push notification contains unhandled deep link: {Uri}", deepLinkUri);
            return;
        }

        logger.LogInformation("Navigating via push deep link: {Uri}", deepLinkUri);
        await deepLinkService.HandleDeepLinkAsync(deepLinkUri);
    }

    /// <summary>
    /// Called when a push notification is received while app is in foreground
    /// </summary>
    public async Task OnReceived(PushNotification notification)
    {
        var (title, message) = ExtractNotificationContent(notification);
        logger.LogInformation("Push notification received: {Title} - {Message}", title, message);

#if ANDROID
        // On Android, show local notification when app is in foreground
        // iOS foreground display is handled via IApplePushDelegate.GetPresentationOptions
        if (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(message))
        {
            await ShowLocalNotificationAsync(title, message);
        }
#else
        await Task.CompletedTask;
#endif
    }

#if IOS
    /// <summary>
    /// Show push notifications as banner + sound + list, even when the app is in the foreground
    /// </summary>
    public UNNotificationPresentationOptions? GetPresentationOptions(PushNotification notification)
        => UNNotificationPresentationOptions.Banner |
           UNNotificationPresentationOptions.Sound |
           UNNotificationPresentationOptions.List;

    /// <summary>
    /// Use the Shiny default (NewData) for background fetch results
    /// </summary>
    public UIBackgroundFetchResult? GetFetchResult(PushNotification notification) => null;
#endif

    /// <summary>
    /// Resolves a deep-link URI from the push data payload.
    /// Supports an explicit "deepLink" URI as well as "propertyId"/"foreclosureId" GUIDs.
    /// </summary>
    private static Uri? ResolveDeepLinkUri(IDictionary<string, string>? data)
    {
        var raw = GetDataValue(data, "deepLink")
                  ?? GetDataValue(data, "deeplink")
                  ?? GetDataValue(data, "url");
        if (raw is not null && Uri.TryCreate(raw, UriKind.Absolute, out var uri))
            return uri;

        var propertyId = GetDataValue(data, "propertyId");
        if (propertyId is not null && Guid.TryParse(propertyId, out var propertyGuid))
            return new Uri($"heimatplatz://property/{propertyGuid}");

        var foreclosureId = GetDataValue(data, "foreclosureId");
        if (foreclosureId is not null && Guid.TryParse(foreclosureId, out var foreclosureGuid))
            return new Uri($"heimatplatz://foreclosure/{foreclosureGuid}");

        return null;
    }

    /// <summary>
    /// Extracts title and message from push notification.
    /// Reads from Notification object first (FCM notification payload),
    /// then falls back to Data dictionary (FCM data payload).
    /// </summary>
    private (string title, string message) ExtractNotificationContent(PushNotification notification)
    {
        // Try Notification object first (standard FCM notification)
        var title = notification.Notification?.Title;
        var message = notification.Notification?.Message;

        // Fall back to Data dictionary if Notification is empty
        if (string.IsNullOrEmpty(title))
            title = GetDataValue(notification.Data, "title");
        if (string.IsNullOrEmpty(message))
            message = GetDataValue(notification.Data, "body") ?? GetDataValue(notification.Data, "message");

        return (title ?? loc.DefaultTitle, message ?? string.Empty);
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
            EnsureDefaultChannelExists();

            var notification = new Shiny.Notifications.Notification
            {
                Title = title,
                Message = message,
                Channel = Shiny.Notifications.Channel.Default.Identifier
            };

            await shinyNotificationManager.Send(notification);
            logger.LogInformation("Local notification shown via Shiny: {Title}", title);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to show local notification");
        }
    }

    private bool _channelInitialized;
    private void EnsureDefaultChannelExists()
    {
        if (_channelInitialized) return;

        try
        {
            var existingChannel = shinyNotificationManager.GetChannel(Shiny.Notifications.Channel.Default.Identifier);
            if (existingChannel == null)
            {
                shinyNotificationManager.AddChannel(Shiny.Notifications.Channel.Default);
                logger.LogInformation("Default notification channel created");
            }
            _channelInitialized = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create default notification channel");
        }
    }

    /// <summary>
    /// Called when the device token changes
    /// </summary>
    public async Task OnNewToken(string token)
    {
        logger.LogInformation("New push token received: {Token}", MaskToken(token));

        // Register the new token with our API
        try
        {
            var platform = GetCurrentPlatform();
            var success = await notificationService.RegisterDeviceAsync(token, platform, CancellationToken.None);

            if (success)
            {
                logger.LogInformation("Device token registered successfully with API");
            }
            else
            {
                logger.LogWarning("Failed to register device token with API");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering device token");
        }
    }

    /// <summary>
    /// Called when push notifications are unregistered
    /// </summary>
    public Task OnUnRegistered(string token)
    {
        logger.LogInformation("Push notifications unregistered for token: {Token}", MaskToken(token));
        return Task.CompletedTask;
    }

    private static string MaskToken(string token)
        => token.Length <= 8 ? "***" : $"{token[..4]}…{token[^4..]}";

    /// <summary>
    /// Gets the current platform identifier
    /// </summary>
    private static string GetCurrentPlatform()
    {
#if ANDROID
        return "Android";
#else
        return "iOS";
#endif
    }
}
#endif
