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
        ("GetSellerSourcesHttpRequest", 3_600),
        ("GetImprintHttpRequest", 86_400),
        ("GetPrivacyPolicyHttpRequest", 86_400),
        ("GetForeclosureAuctionsHttpRequest", 300),
        ("GetForeclosureAuctionByIdHttpRequest", 300),
        ("GetForeclosureAuctionChangesHttpRequest", 300)
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
