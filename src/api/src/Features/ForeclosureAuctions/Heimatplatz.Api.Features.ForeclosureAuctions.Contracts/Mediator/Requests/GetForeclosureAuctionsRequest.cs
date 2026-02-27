using Shiny.Mediator;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Abrufen aller Zwangsversteigerungen mit optionalen Filtern
/// </summary>
public record GetForeclosureAuctionsRequest(
    int Page = 1,
    int PageSize = 25,
    PropertyCategory? Category = null,
    string? City = null,
    string? PostalCode = null,
    DateTimeOffset? AuctionDateFrom = null,
    DateTimeOffset? AuctionDateTo = null,
    decimal? MaxEstimatedValue = null,
    string? Status = null,
    AustrianState? State = null,
    bool? IsActive = null
) : IRequest<GetForeclosureAuctionsResponse>;

/// <summary>
/// Response mit Zwangsversteigerungen (paginiert)
/// </summary>
public record GetForeclosureAuctionsResponse
{
    public required List<ForeclosureAuctionDto> Auctions { get; init; }
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

/// <summary>
/// DTO fuer Zwangsversteigerungs-Details
/// </summary>
public record ForeclosureAuctionDto
{
    public required Guid Id { get; init; }
    public required DateTimeOffset AuctionDate { get; init; }
    public required PropertyCategory Category { get; init; }
    public required string ObjectDescription { get; init; }
    public string? Status { get; init; }

    // Adressdaten
    public required string Address { get; init; }
    public required string City { get; init; }
    public required string PostalCode { get; init; }

    // Grundbuch-Daten
    public string? RegistrationNumber { get; init; }
    public string? CadastralMunicipality { get; init; }
    public string? PlotNumber { get; init; }
    public string? SheetNumber { get; init; }

    // Flaechendaten
    public decimal? TotalArea { get; init; }
    public decimal? BuildingArea { get; init; }
    public decimal? GardenArea { get; init; }
    public decimal? PlotArea { get; init; }

    // Immobilien-Details
    public int? YearBuilt { get; init; }
    public int? NumberOfRooms { get; init; }
    public string? ZoningDesignation { get; init; }
    public string? BuildingCondition { get; init; }

    // Versteigerungs-Details
    public decimal? EstimatedValue { get; init; }
    public decimal? MinimumBid { get; init; }
    public DateTimeOffset? ViewingDate { get; init; }
    public DateTimeOffset? BiddingDeadline { get; init; }
    public string? OwnershipShare { get; init; }

    // Rechtliche Daten
    public string? CaseNumber { get; init; }
    public string? Court { get; init; }
    public string? EdictUrl { get; init; }
    public string? Notes { get; init; }

    // Dokumente
    public string? FloorPlanUrl { get; init; }
    public string? SitePlanUrl { get; init; }
    public string? LongAppraisalUrl { get; init; }
    public string? ShortAppraisalUrl { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    // Scraping-Daten
    public string? ExternalId { get; init; }
    public AustrianState? State { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? FirstSeenAt { get; init; }
    public DateTimeOffset? LastScrapedAt { get; init; }
    public DateTimeOffset? RemovedAt { get; init; }
}
