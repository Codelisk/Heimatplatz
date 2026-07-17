namespace Heimatplatz.Api.Features.ForeclosureAuctions.Configuration;

public class ScrapingOptions
{
    public const string SectionName = "ForeclosureAuctions:Scraping";

    public string BaseUrl { get; set; } = "https://edikte.justiz.gv.at";
    public int TimeoutSeconds { get; set; } = 30;
    public int DelayBetweenRequestsMs { get; set; } = 1000;

    /// <summary>
    /// Extrahiert Fotos aus dem Langgutachten, wenn die separat verlinkten
    /// Edikt-Bilder keine brauchbare Hero-Aufloesung besitzen.
    /// </summary>
    public bool EnablePdfImageFallback { get; set; } = true;

    /// <summary>Harte Download-Grenze fuer ein Langgutachten (Default: 50 MB).</summary>
    public int MaxAppraisalPdfBytes { get; set; } = 50 * 1024 * 1024;

    /// <summary>Maximale Zahl ausgegebener Fotos pro Versteigerung.</summary>
    public int MaxImagesPerAuction { get; set; } = 20;

    /// <summary>
    /// Intervall des automatischen Hintergrund-Syncs (ForeclosureAuctionSyncWorker) in Stunden.
    /// 0 oder negativ (Default) deaktiviert die automatische Ausfuehrung - der Sync wird
    /// bewusst nur manuell ausgeloest (interner IP-gesperrter Bereich unter /intern auf
    /// heimatplatz.at bzw. POST /api/foreclosure-auctions/sync).
    /// </summary>
    public int SyncIntervalHours { get; set; } = 0;

    /// <summary>
    /// Shared-Key fuer den manuellen Sync-Trigger (POST /api/foreclosure-auctions/sync,
    /// Header X-Sync-Key). Leer/nicht gesetzt = Endpoint ist ausserhalb von Development
    /// gesperrt (fail-closed). Wert kommt per Env ForeclosureAuctions__Scraping__SyncTriggerKey
    /// (siehe deploy/hetzner/.env.example, SYNC_TRIGGER_KEY).
    /// </summary>
    public string? SyncTriggerKey { get; set; }

    /// <summary>
    /// Bundesland-Code fuer die Suche (3 = Oberoesterreich).
    /// Codes laut Suchformular der Ediktsdatei (select name="BL"):
    /// 0=Wien, 1=Niederoesterreich, 2=Burgenland, 3=Oberoesterreich, 4=Salzburg,
    /// 5=Steiermark, 6=Kaernten, 7=Tirol, 8=Vorarlberg
    /// </summary>
    public int? BundeslandCode { get; set; } = 3;

    /// <summary>
    /// Welche PropertyCategory-Werte beim Sync AUSGESCHLOSSEN werden sollen.
    /// Default: Wohnungseigentum wird ausgeschlossen (nur Haeuser und Grundstuecke).
    /// </summary>
    public List<string> ExcludedCategories { get; set; } =
    [
        "Wohnungseigentumsobjekt",
        "Eigentumswohnung",
        "Maisonette",
        "Dachterrassenwohnung",
        "Dachgeschoßwohnung",
        "Garconniere",
        "Gartenwohnung"
    ];
}
