using Microsoft.Extensions.Configuration;

namespace Heimatplatz.Maui.Offline;

/// <summary>
/// Explizite Liste der HTTP-Requests, die lokal verfuegbar sein duerfen.
/// Wildcards werden bewusst vermieden, damit niemals schreibende Requests im
/// Offline- oder Cache-Middleware landen.
/// </summary>
internal static class OfflineDataConfiguration
{
    private const string GeneratedNamespace = "Heimatplatz.Maui.ApiClient.Generated";

    // Immobilien-Requests werden vom PropertySyncService per Delta-Sync aktuell gehalten
    // (nur Geaendertes wird nachgeladen) - die Refresh-Fenster hier sind nur noch das
    // Sicherheitsnetz, falls der Sync laengere Zeit nicht laufen konnte.
    //
    // Stammdaten (Locations, Imprint, PrivacyPolicy) revalidieren per Conditional GET
    // (StammdatenConditionalGetHandler): der Hintergrund-Refresh kostet bei
    // unveraendertem Stand nur einen 304-Roundtrip ohne Body. RefreshAfterSeconds
    // ist dort also die Revalidierungs-Frequenz, nicht die Neuladen-Frequenz.
    private static readonly (string Request, int RefreshAfterSeconds)[] Requests =
    [
        ("GetPropertiesHttpRequest", 900),
        ("GetPropertyByIdHttpRequest", 900),
        ("GetUserPropertiesHttpRequest", 900),
        ("GetUserFavoritesHttpRequest", 900),
        ("GetUserBlockedHttpRequest", 900),
        ("GetUserFilterPreferencesHttpRequest", 60),
        ("GetNotificationPreferencesHttpRequest", 60),
        ("GetLocationsHttpRequest", 86_400),
        ("GetImprintHttpRequest", 3_600),
        ("GetPrivacyPolicyHttpRequest", 3_600)
    ];

    public static void AddTo(IDictionary<string, string?> values)
    {
        foreach (var (request, refreshAfterSeconds) in Requests)
        {
            var typeName = $"{GeneratedNamespace}.{request}";

            // UseMaui: Bei fehlendem Internet den letzten erfolgreichen Stand liefern.
            values[$"Mediator:Offline:{typeName}"] = bool.TrueString;

            // Der persistente Cache bleibt gueltig. LocalFirst aktualisiert ihn nach
            // RefreshAfterSeconds im Hintergrund oder explizit bei Pull-to-Refresh.
            values[$"Mediator:Cache:{typeName}:AbsoluteExpirationSeconds"] = "0";
            values[$"Mediator:Cache:{typeName}:SlidingExpirationSeconds"] = "0";
            values[$"Mediator:LocalFirst:{typeName}:RefreshAfterSeconds"] =
                refreshAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
