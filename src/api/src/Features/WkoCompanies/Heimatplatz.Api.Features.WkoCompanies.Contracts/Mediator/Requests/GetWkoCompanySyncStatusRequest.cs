using Shiny.Mediator;

namespace Heimatplatz.Api.Features.WkoCompanies.Contracts.Mediator.Requests;

public record GetWkoCompanySyncStatusRequest : IRequest<GetWkoCompanySyncStatusResponse>;

public record GetWkoCompanySyncStatusResponse
{
    public DateTimeOffset? LastSyncAt { get; init; }
    public int TotalActiveCompanies { get; init; }
    public int TotalRemovedCompanies { get; init; }
}
