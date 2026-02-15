#if __IOS__
using Foundation;
using Shiny;
using UIKit;

namespace Heimatplatz.App.iOS;

/// <summary>
/// Custom AppDelegate that integrates Shiny push notification lifecycle events
/// </summary>
#pragma warning disable CA1422 // Validate platform compatibility - ContinueUserActivity is obsolete in iOS 26 but still functional
public class ShinyAppDelegate : Uno.UI.Runtime.Skia.AppleUIKit.UnoUIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        Console.WriteLine("[ShinyAppDelegate] FinishedLaunching called");
        return base.FinishedLaunching(application, launchOptions);
    }

    public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
    {
        Console.WriteLine($"[ShinyAppDelegate] RegisteredForRemoteNotifications called with token length: {deviceToken?.Length ?? 0}");
        try
        {
            IosShinyHost.OnRegisteredForRemoteNotifications(deviceToken!);
            Console.WriteLine("[ShinyAppDelegate] OnRegisteredForRemoteNotifications completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShinyAppDelegate] OnRegisteredForRemoteNotifications ERROR: {ex}");
        }
    }

    public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
    {
        Console.WriteLine($"[ShinyAppDelegate] FailedToRegisterForRemoteNotifications: {error}");
        try
        {
            IosShinyHost.OnFailedToRegisterForRemoteNotifications(error);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShinyAppDelegate] OnFailedToRegisterForRemoteNotifications ERROR: {ex}");
        }
    }

    public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
    {
        Console.WriteLine("[ShinyAppDelegate] DidReceiveRemoteNotification called");
        IosShinyHost.OnDidReceiveRemoteNotification(userInfo, completionHandler);
    }

    public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
    {
        if (IosShinyHost.OnContinueUserActivity(userActivity, completionHandler))
            return true;

        return base.ContinueUserActivity(application, userActivity, completionHandler);
    }

    public override void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler)
    {
        if (!IosShinyHost.OnHandleEventsForBackgroundUrl(sessionIdentifier, completionHandler))
        {
            base.HandleEventsForBackgroundUrl(application, sessionIdentifier, completionHandler);
        }
    }
}
#pragma warning restore CA1422
#endif
