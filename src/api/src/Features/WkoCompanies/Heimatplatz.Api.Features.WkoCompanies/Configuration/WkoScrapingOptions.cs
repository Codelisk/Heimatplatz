namespace Heimatplatz.Api.Features.WkoCompanies.Configuration;

public class WkoScrapingOptions
{
    public const string SectionName = "WkoCompanies:Scraping";

    public string BaseUrl { get; set; } = "https://firmen.wko.at";
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Wartezeit zwischen einzelnen Requests (Listen-Pagination UND Detailseiten). firmen.wko.at
    /// antwortet bei zu dichten Requests mit HTTP 429 - deutlich konservativer als der
    /// Edikte-Sync (1000ms), weil das beim manuellen Testen bereits nach wenigen Requests auftrat.
    /// </summary>
    public int DelayBetweenRequestsMs { get; set; } = 1500;

    /// <summary>
    /// Sicherheitsgrenze fuer "Mehr laden"-Postbacks pro Suchbegriff (verhindert eine
    /// Endlosschleife, falls das Ergebnis-Panel nie ohne Button zurueckkommt).
    /// </summary>
    public int MaxPagesPerKeyword { get; set; } = 50;

    /// <summary>
    /// Region fuer die Suche, wie im URL-Pfad von firmen.wko.at verwendet
    /// (z.B. "https://firmen.wko.at/immobilienmakler/oberösterreich").
    /// </summary>
    public string Region { get; set; } = "oberösterreich";

    /// <summary>
    /// Suchbegriffe, die zusammen die Immobilienbranche in der konfigurierten Region abdecken.
    /// Ergebnisse werden ueber alle Suchbegriffe hinweg anhand der WKO-Firmaid dedupliziert.
    /// </summary>
    public List<string> BranchKeywords { get; set; } =
    [
        "Immobilienmakler",
        "Immobilienmaklerin",
        "Immobilientreuhänder",
        "Immobilientreuhänderin",
        "Immobilienverwaltung",
        "Immobilienbüro"
    ];

    /// <summary>
    /// Inkrementeller Modus (Default): Firmen, deren WKO-Firmaid bereits in der Datenbank
    /// steht, bekommen beim erneuten Sync KEINEN Detailseiten-Request mehr - nur neue Firmen
    /// werden voll gescraped, Bestandsfirmen werden lediglich als "weiterhin gelistet"
    /// markiert (LastScrapedAt/Reaktivierung). Spart bei ~1000+ Treffern fast alle Requests.
    /// false = jeder Lauf laedt alle Detailseiten neu und aktualisiert geaenderte Firmen
    /// (Content-Hash) - gelegentlich sinnvoll, um Kontaktdaten-Aenderungen nachzuziehen.
    /// </summary>
    public bool SkipDetailsForKnownCompanies { get; set; } = true;

    /// <summary>
    /// Intervall des automatischen Hintergrund-Syncs (WkoCompanySyncWorker) in Stunden.
    /// 0 oder negativ (Default) deaktiviert die automatische Ausfuehrung - der Sync wird
    /// bewusst nur manuell ausgeloest (analog ForeclosureAuctions).
    /// </summary>
    public int SyncIntervalHours { get; set; } = 0;

    /// <summary>
    /// Shared-Key fuer den manuellen Sync-Trigger (POST /api/wko-companies/sync,
    /// Header X-Sync-Key). Leer/nicht gesetzt = Endpoint ist ausserhalb von Development
    /// gesperrt (fail-closed).
    /// </summary>
    public string? SyncTriggerKey { get; set; }
}
