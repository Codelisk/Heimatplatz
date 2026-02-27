using Heimatplatz.Api;
using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.ForeclosureAuctions.Services;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/foreclosure-auctions")]
public class TriggerForeclosureAuctionSyncHandler(
    IForeclosureAuctionSyncService syncService
) : IRequestHandler<TriggerForeclosureAuctionSyncRequest, TriggerForeclosureAuctionSyncResponse>
{
    [MediatorHttpPost("/sync", OperationId = "TriggerForeclosureAuctionSync")]
    public async Task<TriggerForeclosureAuctionSyncResponse> Handle(
        TriggerForeclosureAuctionSyncRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var result = await syncService.SyncAllAsync(cancellationToken);

        return new TriggerForeclosureAuctionSyncResponse
        {
            Created = result.Created,
            Updated = result.Updated,
            Removed = result.Removed,
            Unchanged = result.Unchanged,
            Errors = result.Errors,
            ErrorMessages = result.ErrorMessages
        };
    }
}
