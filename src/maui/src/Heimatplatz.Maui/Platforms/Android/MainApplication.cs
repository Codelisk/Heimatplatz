using Android.App;
using Android.Runtime;

namespace Heimatplatz.Maui;

#if DEBUG
// Debug: Cleartext-HTTP erlauben, damit die App im Emulator die lokale API
// (http://10.0.2.2) erreichen kann. Release bleibt HTTPS-only.
[Application(UsesCleartextTraffic = true)]
#else
[Application]
#endif
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp()
	{
		// Per-App-Locale auf Deutsch: Die App ist rein deutschsprachig, aber
		// SYSTEM-Strings in App-Dialogen (z.B. der "Cancel"-Button des Picker-Dialogs)
		// kommen aus den Android-Ressourcen und folgen sonst der Geraetesprache.
		// Ab API 33 direkt ueber den Framework-LocaleManager (persistiert systemseitig;
		// der AppCompat-Weg griff hier ohne autoStoreLocales nicht), davor best effort
		// ueber AppCompat. Vor der Activity-Erzeugung gesetzt => kein Recreate.
		if (OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			if (GetSystemService(Android.Content.Context.LocaleService) is Android.App.LocaleManager localeManager)
				localeManager.ApplicationLocales = Android.OS.LocaleList.ForLanguageTags("de-AT");
		}
		else if (AndroidX.Core.OS.LocaleListCompat.ForLanguageTags("de-AT") is { } germanLocales)
		{
			AndroidX.AppCompat.App.AppCompatDelegate.ApplicationLocales = germanLocales;
		}

		// Screenshot-Runs (Cake "AndroidScreenshots"): debug.heimatplatz.*-Sysprops in
		// Env-Vars uebersetzen, BEVOR CreateMauiApp die API-URL fixiert. No-op ausserhalb
		// des Emulators.
		Platforms.Android.ScreenshotSysProps.TryImport();

#if DEBUG
		// WebView-Inhalte (Kartenansicht) via chrome://inspect debuggbar machen
		Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
#endif
		return MauiProgram.CreateMauiApp();
	}
}
