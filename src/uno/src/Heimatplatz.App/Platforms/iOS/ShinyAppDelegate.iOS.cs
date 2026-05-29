#if __IOS__
using Foundation;
using UIKit;
using ShinyHost = Shiny.Hosting.Host;

namespace Heimatplatz.App.iOS;

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
            ShinyHost.Lifecycle.OnRegisteredForRemoteNotifications(deviceToken!);
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
            ShinyHost.Lifecycle.OnFailedToRegisterForRemoteNotifications(error);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShinyAppDelegate] OnFailedToRegisterForRemoteNotifications ERROR: {ex}");
        }
    }

    public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
    {
        Console.WriteLine("[ShinyAppDelegate] DidReceiveRemoteNotification called");
        ShinyHost.Lifecycle.OnDidReceiveRemoteNotification(userInfo, completionHandler);
    }
}
#endif
