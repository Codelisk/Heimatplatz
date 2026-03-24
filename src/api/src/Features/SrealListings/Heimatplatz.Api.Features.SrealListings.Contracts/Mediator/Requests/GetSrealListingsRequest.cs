using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.SrealListings.Contracts.Mediator.Requests;

public record GetSrealListingsRequest(
    int Page = 1,
    int PageSize = 25,
    SrealObjectType? ObjectType = null,
    string? City = null,
    string? PostalCode = null,
    decimal? MaxPrice = null,
    AustrianState? State = null,
    bool? IsActive = null
) : IRequest<GetSrealListingsResponse>;

public record GetSrealListingsResponse
{
    public required List<SrealListingDto> Listings { get; init; }
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public record SrealListingDto
{
    public required Guid Id { get; init; }
    public required string ExternalId { get; init; }
    public required string Title { get; init; }
    public required string Address { get; init; }
    public required string City { get; init; }
    public required string PostalCode { get; init; }
    public string? District { get; init; }
    public AustrianState? State { get; init; }
    public SrealObjectType ObjectType { get; init; }
    public string? BuyingType { get; init; }
    public decimal? Price { get; init; }
    public string? PriceText { get; init; }
    public string? Commission { get; init; }
    public decimal? LivingArea { get; init; }
    public decimal? PlotArea { get; init; }
    public int? Rooms { get; init; }
    public string? Description { get; init; }
    public string? EnergyClass { get; init; }
    public string? EnergyValue { get; init; }
    public string? AgentName { get; init; }
    public string? AgentPhone { get; init; }
    public string? AgentEmail { get; init; }
    public string? AgentOffice { get; init; }
    public List<string> ImageUrls { get; init; } = [];
    public required string SourceUrl { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? FirstSeenAt { get; init; }
    public DateTimeOffset? LastScrapedAt { get; init; }
    public DateTimeOffset? RemovedAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
