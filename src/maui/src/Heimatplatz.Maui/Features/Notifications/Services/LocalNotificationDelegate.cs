#if ANDROID || IOS
using Heimatplatz.Maui.Core.DeepLink;
using Microsoft.Extensions.Logging;
using Shiny.Notifications;

namespace Heimatplatz.Maui.Features.Notifications.Services;

/// <summary>
/// Handles taps on local Shiny notifications. On Android these mirror push messages
/// that arrived while the app was in the foreground (PushNotificationDelegate.OnReceived) -
/// the deep link travels in the notification payload so tapping navigates to the
/// property just like a real push tap (IPushDelegate.OnEntry).
/// Registered via AddNotifications&lt;LocalNotificationDelegate&gt;().
/// </summary>
public class LocalNotificationDelegate(
    ILogger<LocalNotificationDelegate> logger,
    IDeepLinkService deepLinkService) : INotificationDelegate
{
    public const string DeepLinkPayloadKey = "deepLink";

    public async Task OnEntry(NotificationResponse response)
    {
        var payload = response.Notification.Payload;
        if (payload is null || !payload.TryGetValue(DeepLinkPayloadKey, out var raw))
            return;

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri) || !deepLinkService.CanHandleUri(uri))
        {
            logger.LogWarning("Local notification contains unhandled deep link: {Raw}", raw);
            return;
        }

        logger.LogInformation("Navigating via local notification deep link: {Uri}", uri);
        await deepLinkService.HandleDeepLinkAsync(uri);
    }
}
#endif
