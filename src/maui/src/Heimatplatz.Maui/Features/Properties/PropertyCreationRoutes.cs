namespace Heimatplatz.Maui.Features.Properties;

/// <summary>
/// Liefert den Standard-Einstieg fuer die Inseratserstellung:
/// Auf Android/iOS-Phones ist der KI-gestuetzte Weg der Primaerweg,
/// auf allen anderen Geraeten die manuelle Erfassung.
/// </summary>
public static class PropertyCreationRoutes
{
    public const string Ai = "AiAddProperty";
    public const string Manual = "AddProperty";

    public static bool IsAiDefault =>
        DeviceInfo.Current.Idiom == DeviceIdiom.Phone
        && (DeviceInfo.Current.Platform == DevicePlatform.Android
            || DeviceInfo.Current.Platform == DevicePlatform.iOS);

    public static string Default => IsAiDefault ? Ai : Manual;
}
