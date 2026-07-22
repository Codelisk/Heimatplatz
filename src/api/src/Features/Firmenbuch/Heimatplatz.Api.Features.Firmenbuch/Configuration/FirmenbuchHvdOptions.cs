namespace Heimatplatz.Api.Features.Firmenbuch.Configuration;

/// <summary>
/// Konfiguration fuer die offizielle "FBW-WebServices (HVD)"-Schnittstelle des
/// Bundesministeriums fuer Justiz (High Value Dataset nach DVO (EU) 2023/138 - im Gegensatz
/// zu openfirmenbuch.at, deren Nutzungsbedingungen automatisiertes Scraping explizit
/// untersagen, ist dieser Weg fuer maschinelle Nutzung vorgesehen). Ohne konfigurierten
/// ApiKey bleibt der Client deaktiviert (kein HTTP-Request) - Aufrufer wie die
/// WkoCompanies-Anreicherung oder der Katalog-Sync laufen dann leer.
/// </summary>
public class FirmenbuchHvdOptions
{
    public const string SectionName = "Firmenbuch:Hvd";

    public string BaseUrl { get; set; } = "https://justizonline.gv.at/jop/api/at.gv.justiz.fbw/ws";

    /// <summary>
    /// X-API-KEY fuer die Schnittstelle. Standardquelle ist der zentrale JustizOnline-
    /// IWG-Zugriffstoken (<c>JustizOnline:IwgApiKey</c>, siehe PostConfigure in
    /// ServiceCollectionExtensions); ein hier gesetzter Wert uebersteuert ihn.
    /// Leer/nicht gesetzt = Anreicherung deaktiviert.
    /// </summary>
    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Wartezeit zwischen Requests (die Schnittstelle liefert bei Ueberlast sauber HTTP 429).</summary>
    public int DelayBetweenRequestsMs { get; set; } = 300;

    /// <summary>
    /// Shared-Key fuer den Katalog-Sync-Trigger (Header X-Sync-Key). Ohne gesetzten Key ist
    /// der Endpoint ausserhalb von Development gesperrt (fail-closed, SharedKeyAuthorization).
    /// </summary>
    public string? SyncTriggerKey { get; set; }
}
