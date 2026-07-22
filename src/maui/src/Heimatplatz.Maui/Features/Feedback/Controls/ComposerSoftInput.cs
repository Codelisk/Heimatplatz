namespace Heimatplatz.Maui.Features.Feedback.Controls;

/// <summary>
/// Android zeigt Seiten standardmaessig mit AdjustPan - die Tastatur schiebt die
/// Seite nur hoch und verdeckt dabei die Messenger-Eingabezeile. Auf den
/// Feedback-Seiten schalten wir das Fenster deshalb auf AdjustResize (Zeile bleibt
/// ueber der Tastatur) und beim Verlassen wieder zurueck, damit der Rest der App
/// unveraendert bleibt. iOS/Windows regeln das selbst - dort no-op.
/// </summary>
internal static class ComposerSoftInput
{
    public static void UseResize()
    {
#if ANDROID
        Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window?
            .SetSoftInputMode(Android.Views.SoftInput.AdjustResize);
#endif
    }

    public static void RestorePan()
    {
#if ANDROID
        Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window?
            .SetSoftInputMode(Android.Views.SoftInput.AdjustPan);
#endif
    }
}
