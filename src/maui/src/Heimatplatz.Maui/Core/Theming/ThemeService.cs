using Shiny;

namespace Heimatplatz.Maui.Core.Theming;

[Singleton]
public class ThemeService : IThemeService
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
        Apply();
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
#endif
    }
}
