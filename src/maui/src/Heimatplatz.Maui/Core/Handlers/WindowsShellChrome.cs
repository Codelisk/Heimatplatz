namespace Heimatplatz.Maui.Core.Handlers;

/// <summary>
/// Faerbt die WinUI-Shell-NavBar (MauiToolbar) in die Markenfarben Paper/OffBlack.
/// Ohne das Mapping bleibt dort ein schwarzer Systembalken ueber der Creme-App:
/// die Toolbar folgt weder Shell.BackgroundColor noch dem UserAppTheme, sondern
/// dem WinUI-Standard-Chrome. Auf Android/iOS existiert das Problem nicht -
/// dort ziehen ThemeService bzw. natives Theme die Leisten bereits nach.
/// </summary>
public static class WindowsShellChrome
{
    public static void Register()
    {
#if WINDOWS
        Microsoft.Maui.Handlers.ToolbarHandler.Mapper.AppendToMapping("HeimatplatzToolbarChrome", (handler, _) =>
        {
            if (handler.PlatformView is not Microsoft.Maui.Platform.MauiToolbar platformToolbar)
                return;

            void Paint()
            {
                var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
                // Farben = Paper / OffBlack (Resources/Styles/Colors.xaml)
                platformToolbar.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(dark
                    ? global::Windows.UI.Color.FromArgb(255, 0x16, 0x10, 0x0D)
                    : global::Windows.UI.Color.FromArgb(255, 0xF6, 0xEC, 0xD8));
            }

            Paint();

            // Theme-Umschalter (Profil-Hero) wechselt zur Laufzeit - die eine
            // App-Toolbar lebt so lange wie die Shell, das Abo leakt nicht.
            if (Application.Current is { } app)
                app.RequestedThemeChanged += (_, _) => Paint();
        });
#endif
    }
}
