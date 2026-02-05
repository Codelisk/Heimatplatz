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
        // Initialize IosShinyHost after services are configured
        // This happens in App.xaml.cs OnLaunched
        return base.FinishedLaunching(application, launchOptions);
    }

    public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
    {
        IosShinyHost.OnRegisteredForRemoteNotifications(deviceToken);
        base.RegisteredForRemoteNotifications(application, deviceToken);
    }

    public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
    {
        IosShinyHost.OnFailedToRegisterForRemoteNotifications(error);
        base.FailedToRegisterForRemoteNotifications(application, error);
    }

    public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
    {
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
