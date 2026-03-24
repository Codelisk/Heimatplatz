using Heimatplatz.Api.Features.SrealListings.Contracts;

namespace Heimatplatz.Api.Features.SrealListings.Services;

/// <summary>
/// Scraper fuer sreal.at Immobilienangebote
/// </summary>
public interface ISrealScraper
{
    /// <summary>
    /// Holt alle Inserate aus der Suchergebnis-Seite (alle Seiten durchpaginiert)
    /// </summary>
    Task<List<SrealListItem>> GetListingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Holt die Detail-Informationen eines einzelnen Inserats
    /// </summary>
    Task<SrealDetail> GetListingDetailAsync(string relativeUrl, CancellationToken ct = default);
}

/// <summary>
/// Eintrag aus der Suchergebnis-Liste
/// </summary>
public record SrealListItem
{
    public required string ExternalId { get; init; }
    public required string DetailUrl { get; init; }
    public required string Title { get; init; }
    public string? Address { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public string? AreaText { get; init; }
    public string? PriceText { get; init; }
    public string? ImageUrl { get; init; }
    public SrealObjectType ObjectType { get; init; }
}

/// <summary>
/// Detail-Informationen eines Inserats
/// </summary>
public record SrealDetail
{
    public required string ExternalId { get; init; }
    public required string Title { get; init; }
    public required string SourceUrl { get; init; }

    // Adresse
    public string? Address { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public string? District { get; init; }

    // Objektdaten
    public SrealObjectType ObjectType { get; init; }
    public string? BuyingType { get; init; }
    public string? PriceText { get; init; }
    public decimal? Price { get; init; }
    public string? Commission { get; init; }
    public decimal? LivingArea { get; init; }
    public decimal? PlotArea { get; init; }
    public int? Rooms { get; init; }
    public string? Description { get; init; }

    // Energieausweis
    public string? EnergyClass { get; init; }
    public string? EnergyValue { get; init; }
    public string? FGee { get; init; }
    public string? FGeeClass { get; init; }

    // Bilder
    public List<string> ImageUrls { get; init; } = [];

    // Makler
    public string? AgentName { get; init; }
    public string? AgentPhone { get; init; }
    public string? AgentEmail { get; init; }
    public string? AgentOffice { get; init; }

    // Infrastruktur
    public string? Infrastructure { get; init; }

    // Alle Felder fuer Hashing
    public Dictionary<string, string> AllFields { get; init; } = new();
}
