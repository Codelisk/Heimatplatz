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

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
