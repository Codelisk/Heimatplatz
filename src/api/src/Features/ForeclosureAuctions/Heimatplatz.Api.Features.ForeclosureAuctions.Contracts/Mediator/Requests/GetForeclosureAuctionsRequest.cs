using Shiny.Mediator;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Abrufen aller Zwangsversteigerungen mit optionalen Filtern
/// </summary>
public record GetForeclosureAuctionsRequest : IRequest<GetForeclosureAuctionsResponse>
{
    /// <summary>Filter nach Bundesland (optional)</summary>
    public AustrianState? State { get; init; }

    /// <summary>Filter nach Kategorie (optional)</summary>
    public PropertyCategory? Category { get; init; }

    /// <summary>Filter nach Ort (optional)</summary>
    public string? City { get; init; }

    /// <summary>Filter nach Postleitzahl (optional)</summary>
    public string? PostalCode { get; init; }

    /// <summary>Filter nach Versteigerungsdatum ab (optional)</summary>
    public DateTimeOffset? AuctionDateFrom { get; init; }

    /// <summary>Filter nach Versteigerungsdatum bis (optional)</summary>
    public DateTimeOffset? AuctionDateTo { get; init; }

    /// <summary>Maximaler geschaetzter Wert (optional)</summary>
    public decimal? MaxEstimatedValue { get; init; }
}

/// <summary>
/// Response mit allen Zwangsversteigerungen
/// </summary>
public record GetForeclosureAuctionsResponse
{
    public required List<ForeclosureAuctionDto> Auctions { get; init; }
}

/// <summary>
/// DTO fuer Zwangsversteigerungs-Details
/// </summary>
public record ForeclosureAuctionDto
{
    public required Guid Id { get; init; }
    public required DateTimeOffset AuctionDate { get; init; }
    public required string Address { get; init; }
    public required string City { get; init; }
    public required string PostalCode { get; init; }
    public required AustrianState State { get; init; }
    public required PropertyCategory Category { get; init; }
    public required string ObjectDescription { get; init; }
    public string? EdictUrl { get; init; }
    public string? Notes { get; init; }
    public decimal? EstimatedValue { get; init; }
    public decimal? MinimumBid { get; init; }
    public string? CaseNumber { get; init; }
    public string? Court { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
