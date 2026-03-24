using Heimatplatz.Api;
using Heimatplatz.Api.Features.SrealListings.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.SrealListings.Services;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.SrealListings.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/sreal-listings")]
public class TriggerSrealSyncHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<TriggerSrealSyncHandler> logger
) : IRequestHandler<TriggerSrealSyncRequest, TriggerSrealSyncResponse>
{
    [MediatorHttpPost("/sync", OperationId = "TriggerSrealSync")]
    public Task<TriggerSrealSyncResponse> Handle(
        TriggerSrealSyncRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        // Fire-and-forget: run sync in background so the HTTP request returns immediately
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var bgSyncService = scope.ServiceProvider.GetRequiredService<ISrealSyncService>();
                var result = await bgSyncService.SyncAllAsync(CancellationToken.None);
                logger.LogInformation(
                    "[sreal.at Sync] Background sync completed: {Created} created, {Updated} updated, {Removed} removed, {Unchanged} unchanged, {Errors} errors",
                    result.Created, result.Updated, result.Removed, result.Unchanged, result.Errors);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[sreal.at Sync] Background sync failed");
            }
        });

        return Task.FromResult(new TriggerSrealSyncResponse
        {
            ErrorMessages = ["Sync started in background"]
        });
    }
}
