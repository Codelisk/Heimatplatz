namespace Heimatplatz.Maui.Core.Handlers;

/// <summary>
/// Entfernt auf Android die native Unterstreichung von Entry/Picker, wenn das Element
/// die StyleClass "borderless" traegt. Gedacht fuer Eingaben, die bereits in einer
/// umrandeten Box (Border) liegen - Suchfelder, Picker in Karten - wo die Android-
/// Unterstreichung als doppelte Dekoration wirkt. Formularfelder ohne Box behalten
/// die Unterstreichung als Felddekoration (Login, Inserat-Formulare).
/// </summary>
public static class BorderlessInputs
{
    public const string StyleClass = "borderless";

    public static void Register()
    {
#if ANDROID
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("HeimatplatzBorderless", (handler, view) =>
        {
            if (view is Entry entry && entry.StyleClass?.Contains(StyleClass) == true)
                handler.PlatformView.Background = null;
        });

        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("HeimatplatzBorderless", (handler, view) =>
        {
            if (view is Picker picker && picker.StyleClass?.Contains(StyleClass) == true)
                handler.PlatformView.Background = null;
        });
#endif
    }
}
