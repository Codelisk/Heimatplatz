using Heimatplatz.Api;
using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.ForeclosureAuctions.Services;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/foreclosure-auctions")]
public class TriggerForeclosureAuctionSyncHandler(
    IForeclosureAuctionSyncService syncService,
    ILogger<TriggerForeclosureAuctionSyncHandler> logger
) : IRequestHandler<TriggerForeclosureAuctionSyncRequest, TriggerForeclosureAuctionSyncResponse>
{
    [MediatorHttpPost("/sync", OperationId = "TriggerForeclosureAuctionSync")]
    public async Task<TriggerForeclosureAuctionSyncResponse> Handle(
        TriggerForeclosureAuctionSyncRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        try
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
        catch (Exception ex)
        {
            logger.LogError(ex, "[Sync] Unhandled error during foreclosure sync");
            return new TriggerForeclosureAuctionSyncResponse
            {
                Errors = 1,
                ErrorMessages = [$"{ex.GetType().Name}: {ex.Message}", ex.InnerException?.Message ?? ""]
            };
        }
    }
}
