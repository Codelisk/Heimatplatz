using Android.App;
using Android.Content;
using Android.Content.PM;

namespace Heimatplatz.Maui;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    Exported = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
// Push-Notification-Klick (Shiny.Push)
[IntentFilter(
    ["SHINY_PUSH_NOTIFICATION_CLICK"],
    Categories = ["android.intent.category.DEFAULT"]
)]
// Deep Link: heimatplatz://property/{guid}
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
    DataScheme = "heimatplatz",
    DataHost = "property"
)]
// Deep Link: heimatplatz://foreclosure/{guid}
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
    DataScheme = "heimatplatz",
    DataHost = "foreclosure"
)]
public class MainActivity : MauiAppCompatActivity
{
    // Kein manueller Shiny-Lifecycle-Code noetig:
    // Shiny.Hosting.Maui (UseShiny) verdrahtet OnCreate/OnNewIntent/OnActivityResult/
    // OnRequestPermissionsResult automatisch ueber die MAUI-Lifecycle-Events.

    // UiMode bleibt in ConfigurationChanges: MAUI aktualisiert damit die
    // AppThemeBindings in-place. Die Activity darf dabei nicht Recreate() ausführen,
    // weil Android sonst den kompletten Shell-Navigationsstack verwirft.
}
