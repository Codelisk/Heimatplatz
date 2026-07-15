using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.ForeclosureAuctions.Services;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/foreclosure-auctions")]
public class TriggerForeclosureAuctionSyncHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<TriggerForeclosureAuctionSyncHandler> logger
) : IRequestHandler<TriggerForeclosureAuctionSyncRequest, TriggerForeclosureAuctionSyncResponse>
{
    // War zuvor komplett unauthentifiziert oeffentlich aufrufbar (jeder im Internet konnte den
    // Scraper anstossen). Der Sync laeuft jetzt primaer automatisch (ForeclosureAuctionSyncWorker);
    // der manuelle Trigger bleibt fuer Admin-Zwecke, braucht aber mindestens einen angemeldeten
    // Benutzer - kein dedizierter Admin-Policy vorhanden, RequireAnyRole ist die naechstbeste Stufe.
    [MediatorHttpPost("/sync", OperationId = "TriggerForeclosureAuctionSync",
        RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireAnyRole])]
    public Task<TriggerForeclosureAuctionSyncResponse> Handle(
        TriggerForeclosureAuctionSyncRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        // Fire-and-forget: run sync in background so the HTTP request returns immediately
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var bgSyncService = scope.ServiceProvider.GetRequiredService<IForeclosureAuctionSyncService>();
                var result = await bgSyncService.SyncAllAsync(CancellationToken.None);
                logger.LogInformation(
                    "[Sync] Background sync completed: {Created} created, {Updated} updated, {Removed} removed, {Unchanged} unchanged, {Errors} errors",
                    result.Created, result.Updated, result.Removed, result.Unchanged, result.Errors);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Sync] Background sync failed");
            }
        });

        return Task.FromResult(new TriggerForeclosureAuctionSyncResponse
        {
            ErrorMessages = ["Sync started in background"]
        });
    }
}
