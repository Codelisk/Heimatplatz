namespace Heimatplatz.Api.Features.WkoCompanies.Configuration;

/// <summary>
/// Konfiguration fuer die Anreicherung von WkoCompany-Datensaetzen ueber die offizielle
/// "FBW-WebServices (HVD)"-Schnittstelle des Bundesministeriums fuer Justiz
/// (High Value Dataset nach DVO (EU) 2023/138 - im Gegensatz zu openfirmenbuch.at, deren
/// Nutzungsbedingungen automatisiertes Scraping explizit untersagen, ist dieser Weg fuer
/// maschinelle Nutzung vorgesehen). Ohne konfigurierten ApiKey bleibt die Anreicherung
/// deaktiviert - der Sync laeuft dann unveraendert weiter, nur ohne die zusaetzlichen Felder.
/// </summary>
public class FirmenbuchHvdOptions
{
    public const string SectionName = "WkoCompanies:FirmenbuchHvd";

    public string BaseUrl { get; set; } = "https://justizonline.gv.at/jop/api/at.gv.justiz.fbw/ws";

    /// <summary>
    /// X-API-KEY fuer die Schnittstelle. Standardquelle ist der zentrale JustizOnline-
    /// IWG-Zugriffstoken (<c>JustizOnline:IwgApiKey</c>, siehe PostConfigure in
    /// ServiceCollectionExtensions); ein hier gesetzter Wert uebersteuert ihn.
    /// Leer/nicht gesetzt = Anreicherung deaktiviert.
    /// </summary>
    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Wartezeit zwischen Anreicherungs-Requests (die Schnittstelle liefert bei Ueberlast sauber HTTP 429).</summary>
    public int DelayBetweenRequestsMs { get; set; } = 300;
}
