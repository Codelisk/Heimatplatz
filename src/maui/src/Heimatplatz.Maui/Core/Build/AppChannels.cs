namespace Heimatplatz.Maui.Core.Build;

/// <summary>Auslieferungskanal des laufenden Builds</summary>
public enum AppChannelKind
{
    /// <summary>Lokaler Entwicklungs-Build (Debug-Konfiguration)</summary>
    Development,

    /// <summary>
    /// Interne Vorab-Auslieferung: Play-Test-Tracks (internal/alpha/beta),
    /// TestFlight sowie Ad-hoc-/Enterprise-signierte iOS-Builds.
    /// </summary>
    Internal,

    /// <summary>Oeffentliche Store-Auslieferung an Endkunden</summary>
    Production
}

/// <summary>
/// Ermittelt einmalig, in welchem Auslieferungskanal die laufende App steckt.
/// Einziger Schalter fuer Werkzeuge, die Endkunden nie sehen duerfen
/// (Flyout-Eintrag "Debug", API-Endpunkt-Umschalter, Test-Anmeldungen).
///
/// Die Erkennung laeuft plattformabhaengig, weil sich die beiden Stores
/// grundlegend unterscheiden:
///
/// - <b>Android:</b> Play liefert dieselbe AAB an jeden Track aus, ein Binary
///   kennt seinen Track zur Laufzeit also NICHT. Der Kanal kommt deshalb aus
///   der Build-Konstante HEIMATPLATZ_INTERNAL, die der Release-Lauf setzt,
///   sobald nicht auf den Production-Track veroeffentlicht wird
///   (siehe cake/Tasks/BuildAndroidTask.cs).
///
/// - <b>iOS:</b> Ein TestFlight-Build wird spaeter unveraendert zur
///   App-Store-Version befoerdert - eine Build-Konstante wuerde den Umschalter
///   also in die Endkundenversion tragen. Apple kennzeichnet die Auslieferung
///   aber im Bundle: TestFlight legt einen "sandboxReceipt" ab, Ad-hoc-/
///   Enterprise-Builds eine embedded.mobileprovision. Beides fehlt in einer
///   aus dem Store geladenen App.
///
/// Fail-closed: Was nicht sicher als Vorab-Auslieferung erkannt wird, gilt als
/// Produktion (auch bei Fehlern der Plattformabfragen).
/// </summary>
public static class AppChannels
{
    /// <summary>Kanal dieses Builds (einmal beim ersten Zugriff ermittelt)</summary>
    public static AppChannelKind Current { get; } = Resolve();

    /// <summary>
    /// True, wenn Entwicklerwerkzeuge sichtbar sein duerfen. Einziger Schalter
    /// fuer alles, was Endkunden nicht erreichen duerfen.
    /// </summary>
    public static bool AreDeveloperToolsEnabled => Current != AppChannelKind.Production;

    /// <summary>Kurzname des Kanals fuer Diagnoseanzeigen (Flyout-Fusszeile, Debug-Seite)</summary>
    public static string DisplayName => Current switch
    {
        AppChannelKind.Development => "Entwicklung",
        AppChannelKind.Internal => IsAppleDistribution ? "TestFlight" : "Interner Test",
        _ => "Produktion"
    };

    private static bool IsAppleDistribution =>
        DeviceInfo.Current.Platform == DevicePlatform.iOS ||
        DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst;

    private static AppChannelKind Resolve()
    {
#if DEBUG
        return AppChannelKind.Development;
#elif HEIMATPLATZ_INTERNAL
        // Vom Build-Lauf gesetzt (Android-Test-Tracks, manuelle interne Builds)
        return AppChannelKind.Internal;
#else
        // Release ohne Build-Konstante: nur Apple kann die Vorab-Auslieferung
        // noch zur Laufzeit vom Store-Download unterscheiden
        return IsApplePreReleaseDistribution() ? AppChannelKind.Internal : AppChannelKind.Production;
#endif
    }

    /// <summary>
    /// TestFlight- oder Ad-hoc-/Enterprise-Auslieferung auf Apple-Plattformen.
    /// Store-Downloads liefern hier false, ebenso jede Plattform ausser iOS/Mac Catalyst.
    /// </summary>
    private static bool IsApplePreReleaseDistribution()
    {
#if IOS || MACCATALYST
        try
        {
            var bundle = Foundation.NSBundle.MainBundle;

            // TestFlight-Installationen bekommen einen Sandbox-Beleg statt des
            // regulaeren App-Store-Belegs
            var receiptName = bundle.AppStoreReceiptUrl?.LastPathComponent;
            if (string.Equals(receiptName, "sandboxReceipt", StringComparison.OrdinalIgnoreCase))
                return true;

            // Ad-hoc-/Enterprise-/Development-signierte Builds tragen ihr
            // Provisioning-Profil im Bundle; Store-Downloads haben keines
            return !string.IsNullOrEmpty(bundle.PathForResource("embedded", "mobileprovision"));
        }
        catch
        {
            // Im Zweifel Produktion: lieber ein fehlendes Werkzeug im internen
            // Test als ein sichtbares beim Endkunden
            return false;
        }
#else
        return false;
#endif
    }
}
