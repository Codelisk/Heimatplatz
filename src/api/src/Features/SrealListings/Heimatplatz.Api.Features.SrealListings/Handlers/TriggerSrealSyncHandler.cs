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
    public async Task<TriggerSrealSyncResponse> Handle(
        TriggerSrealSyncRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        // Run synchronously to capture errors in response (useful for debugging)
        try
        {
            using var scope = scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<ISrealSyncService>();
            var result = await syncService.SyncAllAsync(cancellationToken);
            logger.LogInformation(
                "[sreal.at Sync] Sync completed: {Created} created, {Updated} updated, {Removed} removed, {Unchanged} unchanged, {Errors} errors",
                result.Created, result.Updated, result.Removed, result.Unchanged, result.Errors);

            return new TriggerSrealSyncResponse
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
            logger.LogError(ex, "[sreal.at Sync] Sync failed");
            return new TriggerSrealSyncResponse
            {
                Errors = 1,
                ErrorMessages = [$"Sync failed: {ex.Message} | Inner: {ex.InnerException?.Message}"]
            };
        }
    }
}
