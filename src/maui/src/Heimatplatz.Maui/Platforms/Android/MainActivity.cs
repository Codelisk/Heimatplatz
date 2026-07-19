using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;

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

    UiMode _lastNightMode;
    bool _isResumed;
    DateTimeOffset _lastUserThemeChange = DateTimeOffset.MinValue;

    /// <summary>
    /// Kennzeichnet einen direkt in der App ausgeloesten Theme-Wechsel. AppCompat kann
    /// dabei OnPause vor OnConfigurationChanged senden; dieser kurze Zustand darf nicht
    /// wie ein echter Hintergrundwechsel behandelt werden, sonst geht die Shell-Route verloren.
    /// </summary>
    public void MarkUserInitiatedThemeChange()
        => _lastUserThemeChange = DateTimeOffset.UtcNow;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _lastNightMode = (Resources?.Configuration?.UiMode ?? 0) & UiMode.NightMask;
    }

    protected override void OnResume()
    {
        base.OnResume();
        _isResumed = true;
    }

    protected override void OnPause()
    {
        _isResumed = false;
        base.OnPause();
    }

    /// <summary>
    /// ConfigChanges.UiMode MUSS gesetzt bleiben, damit MAUI den Theme-Wechsel mitbekommt
    /// und die AppThemeBindings umschaltet (ohne Flag bleibt der Seiteninhalt im alten Theme).
    /// Native Theme-Attribute (Status-Bar-Scrim aus colorPrimary, dotnet/maui#32987, und
    /// Control-Tints wie RadioButton-Ringe) loesen sich dabei aber NICHT neu auf - der Scrim
    /// bleibt dann sichtbar auf der alten Farbe (heller Manila-Streifen ueber dunkler App).
    /// Fix: Activity neu erstellen, aber NUR im Hintergrund (Auto-Dark ueber Nacht, Wechsel
    /// in den System-Einstellungen - der Normalfall). Ein Recreate setzt die Shell auf die
    /// Startroute zurueck; im Vordergrund wuerde das den Nutzer aus der aktuellen Seite
    /// reissen, dort lassen wir den Streifen bis zum naechsten App-Start stehen.
    /// </summary>
    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);

        var nightMode = newConfig.UiMode & UiMode.NightMask;
        if (nightMode == _lastNightMode)
            return;

        _lastNightMode = nightMode;
        var isRecentUserChange = DateTimeOffset.UtcNow - _lastUserThemeChange < TimeSpan.FromSeconds(3);
        if (!_isResumed && !isRecentUserChange)
            Recreate();
    }
}
