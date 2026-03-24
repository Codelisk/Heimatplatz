using Shiny.Mediator;

namespace Heimatplatz.Api.Features.SrealListings.Contracts.Mediator.Requests;

public record GetSrealSyncStatusRequest : IRequest<GetSrealSyncStatusResponse>;

public record GetSrealSyncStatusResponse
{
    public DateTimeOffset? LastSyncAt { get; init; }
    public int TotalActiveListings { get; init; }
    public int TotalRemovedListings { get; init; }
    public int TotalChanges { get; init; }
}
