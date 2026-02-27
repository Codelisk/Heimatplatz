using Shiny.Mediator;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Contracts.Mediator.Requests;

public record GetForeclosureAuctionChangesRequest(
    int Page = 1,
    int PageSize = 50,
    DateTimeOffset? Since = null,
    string? ChangeType = null
) : IRequest<GetForeclosureAuctionChangesResponse>;

public record GetForeclosureAuctionChangesResponse
{
    public required List<ForeclosureAuctionChangeDto> Changes { get; init; }
    public int TotalCount { get; init; }
}

public record ForeclosureAuctionChangeDto
{
    public required Guid Id { get; init; }
    public required Guid ForeclosureAuctionId { get; init; }
    public required string ChangeType { get; init; }
    public string? ChangedFields { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
