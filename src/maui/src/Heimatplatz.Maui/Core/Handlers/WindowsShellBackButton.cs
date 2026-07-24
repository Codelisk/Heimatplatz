namespace Heimatplatz.Maui.Core.Handlers;

/// <summary>
/// Der WinUI-Shell-Zurueckpfeil (NavigationViewBackButton im NavigationView-Template)
/// ist mit 12px-Glyph und Systemfarbe kaum wahrnehmbar (HP-MAUI-016). Vergroessert
/// Trefferflaeche und Glyph und setzt die Vordergrundfarbe auf die App-Text-Tokens
/// (Gray900/White), theme-reaktiv ueber ActualThemeChanged.
/// </summary>
public static class WindowsShellBackButton
{
#if WINDOWS
    public static void Apply(Shell shell)
    {
        if (shell.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.NavigationView navView)
            return;

        // Template-Kinder existieren erst nach dem nativen Laden; beim erneuten
        // Attach (Loaded feuert mehrfach) ist das Styling idempotent.
        navView.Loaded += (_, _) => TryStyleBackButton(navView);
        TryStyleBackButton(navView);
    }

    private static void TryStyleBackButton(Microsoft.UI.Xaml.Controls.NavigationView navView)
    {
        if (FindDescendantButton(navView, "NavigationViewBackButton") is not { } backButton)
            return;

        backButton.Width = 44;
        backButton.Height = 40;
        backButton.FontSize = 16;

        if (backButton.Tag as string != StyledTag)
        {
            backButton.Tag = StyledTag;
            backButton.ActualThemeChanged += (button, _) => ApplyForeground(button);
        }

        ApplyForeground(backButton);
    }

    private const string StyledTag = "Heimatplatz.BackButtonStyled";

    private static void ApplyForeground(Microsoft.UI.Xaml.FrameworkElement backButton)
    {
        var isDark = backButton.ActualTheme == Microsoft.UI.Xaml.ElementTheme.Dark;
        var resourceKey = isDark ? "White" : "Gray900";

        if (backButton is Microsoft.UI.Xaml.Controls.Button button &&
            Application.Current?.Resources.TryGetValue(resourceKey, out var value) == true &&
            value is Color color)
        {
            button.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.Maui.Platform.ColorExtensions.ToWindowsColor(color));
        }
    }

    private static Microsoft.UI.Xaml.Controls.Button? FindDescendantButton(
        Microsoft.UI.Xaml.DependencyObject root,
        string name)
    {
        var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);

            if (child is Microsoft.UI.Xaml.Controls.Button button && button.Name == name)
                return button;

            if (FindDescendantButton(child, name) is { } nested)
                return nested;
        }

        return null;
    }
#else
    public static void Apply(Shell shell)
    {
        // Nur WinUI braucht die Nachbesserung - Android/iOS zeichnen den
        // Zurueckpfeil ueber die regulaere Shell-Chrome ausreichend deutlich.
        _ = shell;
    }
#endif
}
