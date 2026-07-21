using Foundation;
using Heimatplatz.Maui.Core.DeepLink;
using Microsoft.Extensions.DependencyInjection;
using UIKit;

namespace Heimatplatz.Maui;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	// Push (APNs): Shiny.Hosting.Maui verdrahtet die Remote-Notification-Callbacks
	// NICHT automatisch - die drei Exports muessen wie im offiziellen Shiny-v5-Sample
	// an Shiny.Hosting.Host.Lifecycle weitergeleitet werden. Ohne sie kommt der
	// APNs-Token nie an und Shiny.Push.RequestRawToken laeuft in den 10s-Timeout.

	[Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
	public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
		=> global::Shiny.Hosting.Host.Lifecycle.OnRegisteredForRemoteNotifications(deviceToken);

	[Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
	public void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
		=> global::Shiny.Hosting.Host.Lifecycle.OnFailedToRegisterForRemoteNotifications(error);

	[Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
	public void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
		=> global::Shiny.Hosting.Host.Lifecycle.OnDidReceiveRemoteNotification(userInfo, completionHandler);

	/// <summary>
	/// Custom-URL-Scheme Deep Links (heimatplatz://property/{guid}, heimatplatz://foreclosure/{guid}).
	/// Universal Links liefen ueber ContinueUserActivity und Shiny IIosLifecycle.IContinueActivity.
	/// </summary>
	public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
	{
		var handledByMaui = base.OpenUrl(application, url, options);

		if (url.AbsoluteString is string raw
			&& Uri.TryCreate(raw, UriKind.Absolute, out var uri))
		{
			var deepLinkService = IPlatformApplication.Current?.Services.GetService<IDeepLinkService>();
			if (deepLinkService?.CanHandleUri(uri) == true)
			{
				_ = HandleDeepLinkSafeAsync(deepLinkService, uri);
				return true;
			}
		}

		return handledByMaui;
	}

	private static async Task HandleDeepLinkSafeAsync(IDeepLinkService deepLinkService, Uri uri)
	{
		try
		{
			await deepLinkService.HandleDeepLinkAsync(uri);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[DeepLink] Fehler beim Verarbeiten des Deep Links {uri}: {ex}");
		}
	}
}
