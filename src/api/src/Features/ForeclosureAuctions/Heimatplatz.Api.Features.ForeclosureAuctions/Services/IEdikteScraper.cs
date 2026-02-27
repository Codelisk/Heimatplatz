namespace Heimatplatz.Api.Features.ForeclosureAuctions.Services;

/// <summary>
/// Scraper fuer edikte.justiz.gv.at Zwangsversteigerungen
/// </summary>
public interface IEdikteScraper
{
    /// <summary>
    /// Holt alle aktuellen Zwangsversteigerungen von der Ergebnisliste
    /// </summary>
    Task<List<EdiktListItem>> GetAuctionListAsync(CancellationToken ct = default);

    /// <summary>
    /// Holt die Detail-Informationen eines einzelnen Edikts
    /// </summary>
    Task<EdiktDetail> GetAuctionDetailAsync(string externalId, CancellationToken ct = default);
}

/// <summary>
/// Eintrag aus der Ergebnisliste
/// </summary>
public record EdiktListItem
{
    public required string ExternalId { get; init; }
    public required string DetailUrl { get; init; }
    public required string StatusText { get; init; }
    public string? DateText { get; init; }
    public string? Address { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public string? CategoryText { get; init; }
    public string? ObjectDescription { get; init; }
}

/// <summary>
/// Detail-Informationen eines Edikts
/// </summary>
public record EdiktDetail
{
    public required string ExternalId { get; init; }

    // Gericht & Aktenzeichen
    public string? Court { get; init; }
    public string? CaseNumber { get; init; }
    public string? Reason { get; init; }

    // Termin
    public string? AuctionDateText { get; init; }
    public string? AuctionLocation { get; init; }

    // Grundbuch
    public string? CadastralMunicipality { get; init; }
    public string? RegistrationNumber { get; init; }
    public string? PlotNumber { get; init; }
    public string? SheetNumber { get; init; }

    // Adresse
    public string? Address { get; init; }
    public string? PostalCodeAndCity { get; init; }

    // Objekt
    public string? CategoryText { get; init; }
    public string? ObjectDescription { get; init; }
    public string? PlotAreaText { get; init; }
    public string? ObjectAreaText { get; init; }

    // Werte
    public string? EstimatedValueText { get; init; }
    public string? MinimumBidText { get; init; }
    public string? VadiumText { get; init; }

    // Dokumente
    public string? ShortAppraisalUrl { get; init; }
    public string? LongAppraisalUrl { get; init; }
    public string? SitePlanUrl { get; init; }
    public string? FloorPlanUrl { get; init; }

    // Status
    public string? StatusText { get; init; }
    public string? LastChangeDateText { get; init; }
    public string? PublicationDateText { get; init; }

    // Alle Felder als Dictionary fuer Hashing
    public Dictionary<string, string> AllFields { get; init; } = new();
}
