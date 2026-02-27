namespace Heimatplatz.Api.Features.ForeclosureAuctions.Configuration;

public class ScrapingOptions
{
    public const string SectionName = "ForeclosureAuctions:Scraping";

    public string BaseUrl { get; set; } = "https://edikte.justiz.gv.at";
    public int TimeoutSeconds { get; set; } = 30;
    public int DelayBetweenRequestsMs { get; set; } = 1000;

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
