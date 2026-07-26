using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Maui.Core.Theming;

[Singleton]
public class ThemeService(ILogger<ThemeService> logger) : IThemeService
{
    private const string PreferenceKey = "app.theme-mode";

    public AppThemeMode Mode { get; private set; } = AppThemeMode.System;

    public void Initialize()
    {
        var stored = Preferences.Default.Get(PreferenceKey, nameof(AppThemeMode.System));
        Mode = Enum.TryParse<AppThemeMode>(stored, out var mode) ? mode : AppThemeMode.System;
        Apply();

        // Im System-Modus folgt die App dem Geraete-Theme - die nativen
        // Systemleisten muessen den Wechsel mitmachen
        if (Application.Current is { } app)
            app.RequestedThemeChanged += (_, _) => ApplySystemBars();
    }

    public AppThemeMode CycleMode()
    {
        Mode = Mode switch
        {
            AppThemeMode.System => AppThemeMode.Light,
            AppThemeMode.Light => AppThemeMode.Dark,
            _ => AppThemeMode.System
        };
        Preferences.Default.Set(PreferenceKey, Mode.ToString());

        // Diagnose (Beobachtung 26.07.: nach einem Theme-Wechsel stand die App einmalig
        // auf Home statt auf der vorherigen Seite). Ein Recreate gibt es nicht (UiMode in
        // ConfigurationChanges) - falls die Route trotzdem kippt, soll das Log es zeigen.
        var routeBefore = Shell.Current?.CurrentState?.Location?.ToString();
        Apply();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var routeAfter = Shell.Current?.CurrentState?.Location?.ToString();
            if (routeBefore != routeAfter)
                logger.LogWarning(
                    "Theme-Wechsel auf {Mode} hat die Route veraendert: {Before} -> {After}",
                    Mode, routeBefore, routeAfter);
        });

        return Mode;
    }

    public void Apply()
    {
        if (Application.Current is not { } app)
            return;

        app.UserAppTheme = Mode switch
        {
            AppThemeMode.Light => AppTheme.Light,
            AppThemeMode.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };

        ApplySystemBars();
    }

    public void PrepareWindow(IActivationState? activationState)
    {
#if IOS || MACCATALYST
        // MAUI erzeugt das native UIWindow bereits vor Application.CreateWindow und
        // stellt es im Window-MauiContext bereit. Hier muss der Stil gesetzt werden,
        // BEVOR AppShell und damit UIRefreshControl/UICollectionView/Header entstehen:
        // UIKit loest einige Systemfarben beim Erzeugen konkret auf und zieht einen
        // spaeteren Override dann nicht fuer jede Supplementary View nach.
        if (activationState?.Context.Services.GetService(typeof(UIKit.UIWindow)) is UIKit.UIWindow nativeWindow)
            ApplyNativeWindowTheme(nativeWindow);
#endif
    }

    /// <summary>Effektives Theme (System-Modus aufgeloest auf das Geraete-Theme).</summary>
    private AppTheme EffectiveTheme => Mode switch
    {
        AppThemeMode.Light => AppTheme.Light,
        AppThemeMode.Dark => AppTheme.Dark,
        _ => Application.Current?.PlatformAppTheme ?? AppTheme.Light
    };

    private void ApplySystemBars()
    {
#if ANDROID
        // AppThemeBindings schalten beim UserAppTheme-Wechsel sofort um, die nativen
        // System-Bars nicht: Android malt Status-/Navigationsleiste aus den nativen
        // values/values-night-Ressourcen (colorPrimary-Scrim, dotnet/maui#32987), und
        // die folgen weiter dem GERAETE-Theme. Deshalb hier direkt am Window nachziehen,
        // sonst steht z.B. ein heller Manila-Streifen ueber der dunklen App.
        // Ab Android 15 (Edge-to-Edge) sind die Set*BarColor-Aufrufe No-Ops - dort ist
        // die Leiste transparent und die Shell-Farbe scheint ohnehin durch.
        var window = Platform.CurrentActivity?.Window;
        if (window is null)
            return;

        var dark = EffectiveTheme == AppTheme.Dark;
        // Farben = Paper / OffBlack (Resources/Styles/Colors.xaml)
        var barColor = Android.Graphics.Color.ParseColor(dark ? "#16100D" : "#F6ECD8");
#pragma warning disable CA1422 // ab API 35 obsolet, auf aelteren Geraeten weiter noetig
        window.SetStatusBarColor(barColor);
        window.SetNavigationBarColor(barColor);
#pragma warning restore CA1422

        if (AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView) is { } insets)
        {
            insets.AppearanceLightStatusBars = !dark;
            insets.AppearanceLightNavigationBars = !dark;
        }

        // Edge-to-Edge (API 35+): Der Streifen hinter der Status-Bar ist die
        // Inset-Flaeche der AppBarLayout, deren Hintergrund aus dem NATIVEN Theme
        // (colorPrimary) kommt. Beim UserAppTheme-Wechsel ohne Recreate bleibt der
        // alte Wert stehen, bis die naechste Navigation die Toolbar neu aufbaut -
        // deshalb hier direkt umfaerben.
        if (window.DecorView is Android.Views.ViewGroup decorRoot)
            PaintAppBarLayouts(decorRoot, barColor);
#elif IOS || MACCATALYST
        // MAUI reicht UserAppTheme nur an den gerade aktiven ViewController weiter -
        // beim App-Start existiert der noch nicht. System-gezeichnete Flaechen
        // (Pull-to-Refresh-Control, Navbar-Transluzenz, Tastatur, Alerts) folgen dann
        // weiter dem GERAETE-Theme und erscheinen z.B. hell ueber der dunklen App.
        // Deshalb das erzwungene Theme direkt auf alle nativen Fenster legen.
        // Beide Wege setzen: bei window.Created haengt das UIWindow u.U. noch nicht
        // an der Scene (ConnectedScenes leer), dafuer existiert das PlatformView -
        // spaeter (Activated/Theme-Wechsel) ist es garantiert ueber die Scenes da.
        foreach (var mauiWindow in Application.Current?.Windows ?? [])
        {
            if (mauiWindow.Handler?.PlatformView is UIKit.UIWindow platformWindow)
                ApplyNativeWindowTheme(platformWindow);
        }

        foreach (var scene in UIKit.UIApplication.SharedApplication.ConnectedScenes)
        {
            if (scene is UIKit.UIWindowScene windowScene)
            {
                foreach (var nativeWindow in windowScene.Windows)
                    ApplyNativeWindowTheme(nativeWindow);
            }
        }
#endif
    }

#if ANDROID
    /// <summary>Faerbt alle AppBarLayouts im View-Baum um (Status-Bar-Inset-Flaeche).</summary>
    private static void PaintAppBarLayouts(Android.Views.ViewGroup root, Android.Graphics.Color color)
    {
        for (var i = 0; i < root.ChildCount; i++)
        {
            var child = root.GetChildAt(i);
            if (child is Google.Android.Material.AppBar.AppBarLayout appBar)
                appBar.SetBackgroundColor(color);

            if (child is Android.Views.ViewGroup group)
                PaintAppBarLayouts(group, color);
        }
    }
#endif

#if IOS || MACCATALYST
    private void ApplyNativeWindowTheme(UIKit.UIWindow nativeWindow)
    {
        nativeWindow.OverrideUserInterfaceStyle = Mode switch
        {
            AppThemeMode.Light => UIKit.UIUserInterfaceStyle.Light,
            AppThemeMode.Dark => UIKit.UIUserInterfaceStyle.Dark,
            _ => UIKit.UIUserInterfaceStyle.Unspecified
        };
    }
#endif
}
