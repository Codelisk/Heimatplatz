using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Shiny;

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

        // Shiny lifecycle hook
        AndroidShinyHost.OnActivityOnCreate(this, savedInstanceState);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        // Shiny lifecycle hook
        AndroidShinyHost.OnNewIntent(this, intent);
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        // Shiny lifecycle hook for permission results
        AndroidShinyHost.OnRequestPermissionsResult(this, requestCode, permissions, grantResults);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        // Shiny lifecycle hook
        AndroidShinyHost.OnActivityResult(this, requestCode, resultCode, data);
    }
}
