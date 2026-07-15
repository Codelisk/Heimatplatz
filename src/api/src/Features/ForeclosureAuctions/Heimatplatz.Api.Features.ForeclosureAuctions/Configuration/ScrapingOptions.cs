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
    /// Bundesland-Code fuer die Suche (3 = Oberoesterreich).
    /// Lotus Notes Codes: 1=Burgenland, 2=Kaernten, 3=OOe, 4=NOe, 5=Salzburg, 6=Steiermark, 7=Tirol, 8=Vorarlberg, 9=Wien
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
