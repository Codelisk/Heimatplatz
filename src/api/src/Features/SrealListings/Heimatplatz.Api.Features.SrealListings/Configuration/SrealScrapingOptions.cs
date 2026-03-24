namespace Heimatplatz.Api.Features.SrealListings.Configuration;

public class SrealScrapingOptions
{
    public const string SectionName = "SrealListings:Scraping";

    public string BaseUrl { get; set; } = "https://www.sreal.at";
    public int TimeoutSeconds { get; set; } = 30;
    public int DelayBetweenRequestsMs { get; set; } = 1500;
    public string SearchPath { get; set; } = "/de/immobilien-suche";
    public string BuyingType { get; set; } = "buy";
    public List<string> Locations { get; set; } = ["f_Oberösterreich"];
    public List<string> ObjectTypes { get; set; } = ["vacation", "property", "house"];
    public string Sorting { get; set; } = "updated_desc";
}
