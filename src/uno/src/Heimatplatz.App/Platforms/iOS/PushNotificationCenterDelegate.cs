#if __IOS__
using UserNotifications;

namespace Heimatplatz.App.iOS;

/// <summary>
/// UNUserNotificationCenterDelegate that shows push notifications as banners
/// even when the app is in the foreground. Required because Shiny's IosShinyHost
/// no longer sets this delegate automatically.
/// Also forwards notification taps to Shiny for processing.
/// </summary>
public class PushNotificationCenterDelegate : UNUserNotificationCenterDelegate
{
    public override void WillPresentNotification(
        UNUserNotificationCenter center,
        UNNotification notification,
        Action<UNNotificationPresentationOptions> completionHandler)
    {
        // Always show push notifications as banner + sound + list, even in foreground
        completionHandler(
            UNNotificationPresentationOptions.Banner |
            UNNotificationPresentationOptions.Sound |
            UNNotificationPresentationOptions.List);
    }

    public override void DidReceiveNotificationResponse(
        UNUserNotificationCenter center,
        UNNotificationResponse response,
        Action completionHandler)
    {
        // Notification tap handling is done via DidReceiveRemoteNotification in ShinyAppDelegate
        completionHandler();
    }
}
#endif
