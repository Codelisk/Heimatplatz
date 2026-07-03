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
	// automatisch ueber UseShiny() - kein manueller Code noetig.

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
