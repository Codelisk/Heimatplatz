namespace Heimatplatz.Maui.Core.Handlers;

/// <summary>
/// Entfernt die native Feld-Chrome von Entry/Editor/Picker, wenn das Element die
/// StyleClass "borderless" traegt. Gedacht fuer Eingaben, die bereits in einer
/// umrandeten Box (Border) liegen - Suchfelder, Picker in Karten und die
/// gestrichelten WYSIWYG-Felder des Inserat-Editors - wo die Plattform-Dekoration
/// (Android-Unterstreichung, WinUI-TextBox-Rahmen) als doppelte Box wirkt.
/// Formularfelder ohne Box behalten die Plattform-Dekoration (Login etc.).
/// </summary>
public static class BorderlessInputs
{
    public const string StyleClass = "borderless";

    private static bool IsBorderless(IView view) =>
        view is VisualElement element && element.StyleClass?.Contains(StyleClass) == true;

    public static void Register()
    {
#if ANDROID
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("HeimatplatzBorderless", (handler, view) =>
        {
            if (IsBorderless(view))
                handler.PlatformView.Background = null;
        });

        Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("HeimatplatzBorderless", (handler, view) =>
        {
            if (IsBorderless(view))
                handler.PlatformView.Background = null;
        });

        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("HeimatplatzBorderless", (handler, view) =>
        {
            if (IsBorderless(view))
                handler.PlatformView.Background = null;
        });
#elif WINDOWS
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("HeimatplatzBorderless", (handler, view) =>
        {
            if (IsBorderless(view))
                StripWinUiTextBoxChrome(handler.PlatformView);
        });

        Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("HeimatplatzBorderless", (handler, view) =>
        {
            if (IsBorderless(view))
                StripWinUiTextBoxChrome(handler.PlatformView);
        });
#elif IOS || MACCATALYST
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("HeimatplatzBorderless", (handler, view) =>
        {
            if (IsBorderless(view))
                handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
        });
#endif
    }

#if WINDOWS
    /// <summary>
    /// WinUI-TextBox: Rahmen, Hintergrund und die Theme-Brushes fuer Hover/Fokus
    /// neutralisieren - nur BorderThickness reicht nicht, die Zustaende holen sich
    /// ihre Chrome sonst aus den Theme-Resources zurueck.
    /// </summary>
    private static void StripWinUiTextBoxChrome(Microsoft.UI.Xaml.Controls.TextBox textBox)
    {
        var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);

        textBox.Background = transparent;
        textBox.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
        textBox.Padding = new Microsoft.UI.Xaml.Thickness(0, 4, 0, 4);

        foreach (var key in new[]
        {
            "TextControlBackground",
            "TextControlBackgroundPointerOver",
            "TextControlBackgroundFocused",
            "TextControlBackgroundDisabled",
            "TextControlBorderBrush",
            "TextControlBorderBrushPointerOver",
            "TextControlBorderBrushFocused",
            "TextControlBorderBrushDisabled"
        })
        {
            textBox.Resources[key] = transparent;
        }

        textBox.Resources["TextControlBorderThemeThickness"] = new Microsoft.UI.Xaml.Thickness(0);
        textBox.Resources["TextControlBorderThemeThicknessFocused"] = new Microsoft.UI.Xaml.Thickness(0);
    }
#endif
}
