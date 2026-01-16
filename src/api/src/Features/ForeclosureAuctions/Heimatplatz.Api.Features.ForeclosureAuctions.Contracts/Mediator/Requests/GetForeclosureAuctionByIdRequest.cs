using Shiny.Mediator;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Abrufen einer einzelnen Zwangsversteigerung
/// </summary>
public record GetForeclosureAuctionByIdRequest : IRequest<GetForeclosureAuctionByIdResponse>
{
    public required Guid Id { get; init; }
}

/// <summary>
/// Response mit Zwangsversteigerungs-Details
/// </summary>
public record GetForeclosureAuctionByIdResponse
{
    public ForeclosureAuctionDto? Auction { get; init; }
}
