namespace Heimatplatz.Maui.Features.Feedback.Controls;

/// <summary>
/// Schaltet das Fenster auf den Feedback-Seiten auf AdjustNothing: weder das System
/// (Pan) noch MAUI (Resize/SafeArea) sollen die Eingabezeile bei geoeffneter Tastatur
/// bewegen. Das uebernimmt allein der WindowInsetsAnimation-Callback im
/// <see cref="MessageComposer"/>, der die Zeile Bild fuer Bild mit der Tastatur
/// mitzieht - dadurch fluessig statt Ruck am Animationsende. Beim Verlassen wird der
/// App-Standard (Pan) wiederhergestellt. iOS/Windows regeln das selbst - dort no-op.
/// </summary>
internal static class ComposerSoftInput
{
    public static void Engage()
    {
#if ANDROID
        Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window?
            .SetSoftInputMode(Android.Views.SoftInput.AdjustNothing);
#endif
    }

    public static void Restore()
    {
#if ANDROID
        Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window?
            .SetSoftInputMode(Android.Views.SoftInput.AdjustPan);
#endif
    }
}
