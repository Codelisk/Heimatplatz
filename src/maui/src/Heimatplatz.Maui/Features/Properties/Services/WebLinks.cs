namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Baut Web-Links passend zum AKTIVEN API-Endpunkt. Ein Debug-Build gegen die
/// Test-API wuerde sonst Produktions-Links fuer Inserate teilen, die es auf
/// heimatplatz.at gar nicht gibt. Release-Builds laufen immer gegen die
/// Produktions-API und teilen damit unveraendert heimatplatz.at.
/// </summary>
public static class WebLinks
{
    private const string ProductionWebBase = "https://heimatplatz.at";
    private const string TestWebBase = "https://test.heimatplatz.at";

    /// <summary>Detailseiten-Link eines Inserats im Web-Pendant des API-Endpunkts.</summary>
    public static Uri ListingUrl(string apiBaseUrl, Guid propertyId)
        => new($"{WebBaseFor(apiBaseUrl)}/immobilien/angebote/{propertyId}");

    /// <summary>
    /// Vollbild-Kartenansicht (/karte-embed) im Web-Pendant des API-Endpunkts -
    /// die App zeigt sie im WebView, damit Karte, Papier-Stil und Interaktion
    /// exakt der Web-Faltkarte entsprechen. <paramref name="query"/> kommt aus
    /// <see cref="MapEmbedLink.BuildQuery"/>.
    /// </summary>
    public static Uri MapEmbedUrl(string apiBaseUrl, string query)
        => new($"{WebBaseFor(apiBaseUrl)}/karte-embed?{query}");

    internal static string WebBaseFor(string apiBaseUrl)
    {
        // Produktion nur bei der echten Prod-API; Test-API und lokale Entwicklung
        // (Dev-DB hat dieselben Seed-GUIDs wie Test) zeigen aufs Test-Web.
        var isProduction = apiBaseUrl.Contains("//api.heimatplatz.at", StringComparison.OrdinalIgnoreCase);
        return isProduction ? ProductionWebBase : TestWebBase;
    }
}
