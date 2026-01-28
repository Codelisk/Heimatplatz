using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace Heimatplatz.App.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden
)]
[IntentFilter(
    ["SHINY_PUSH_NOTIFICATION_CLICK"],
    Categories = ["android.intent.category.DEFAULT"]
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        global::AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

        base.OnCreate(savedInstanceState);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        // Handle push notification click intents
        if (intent?.Action == "SHINY_PUSH_NOTIFICATION_CLICK")
        {
            // Intent will be handled by registered delegates
        }
    }
}
