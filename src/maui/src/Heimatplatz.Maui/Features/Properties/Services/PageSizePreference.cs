namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Lokale (geraetegebundene) Einstellung "Objekte pro Seite". Wird auf der
/// FilterSettingsPage geaendert und von der HomePage beim Laden verwendet -
/// beide greifen ueber diese Klasse auf denselben Preferences-Eintrag zu.
/// </summary>
public static class PageSizePreference
{
    public const int Default = 20;

    public static IReadOnlyList<int> Options { get; } = [10, 20, 50];

    private const string Key = "properties.page-size";

    public static int Get()
        => Normalize(Preferences.Default.Get(Key, Default));

    public static void Set(int value)
        => Preferences.Default.Set(Key, Normalize(value));

    public static int Normalize(int value)
        => Options.Contains(value) ? value : Default;
}
