using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Features.WkoCompanies.Configuration;
using Heimatplatz.Api.Features.WkoCompanies.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.WkoCompanies.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.WkoCompanies.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/wko-companies")]
public class TriggerWkoCompanySyncHandler(
    IServiceScopeFactory scopeFactory,
    IHttpContextAccessor httpContextAccessor,
    IOptions<WkoScrapingOptions> scrapingOptions,
    IHostEnvironment environment,
    ILogger<TriggerWkoCompanySyncHandler> logger
) : IRequestHandler<TriggerWkoCompanySyncRequest, TriggerWkoCompanySyncResponse>
{
    public const string TriggerKeyHeader = "X-Sync-Key";

    // 0 = idle, 1 = Sync laeuft. Ohne diesen Guard startet jeder wiederholte Trigger einen
    // weiteren parallelen Scraper-Lauf gegen firmen.wko.at samt konkurrierender DB-Writes.
    private static int _syncRunning;

    [MediatorHttpPost("/sync", OperationId = "TriggerWkoCompanySync")]
    public Task<TriggerWkoCompanySyncResponse> Handle(
        TriggerWkoCompanySyncRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
            throw new UnauthorizedAccessException("Sync-Trigger-Key fehlt oder ist ungueltig.");

        if (Interlocked.CompareExchange(ref _syncRunning, 1, 0) != 0)
        {
            logger.LogInformation("[WkoSync] Trigger ignoriert - Sync laeuft bereits");
            return Task.FromResult(new TriggerWkoCompanySyncResponse
            {
                ErrorMessages = ["Sync laeuft bereits - kein neuer Lauf gestartet"]
            });
        }

        // Fire-and-forget: run sync in background so the HTTP request returns immediately
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var bgSyncService = scope.ServiceProvider.GetRequiredService<IWkoCompanySyncService>();
                var result = await bgSyncService.SyncAllAsync(CancellationToken.None);
                logger.LogInformation(
                    "[WkoSync] Background sync completed: {Created} created, {Updated} updated, {Removed} removed, {Unchanged} unchanged, {Errors} errors",
                    result.Created, result.Updated, result.Removed, result.Unchanged, result.Errors);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[WkoSync] Background sync failed");
            }
            finally
            {
                Interlocked.Exchange(ref _syncRunning, 0);
            }
        });

        return Task.FromResult(new TriggerWkoCompanySyncResponse
        {
            ErrorMessages = ["Sync started in background"]
        });
    }

    private bool IsAuthorized()
    {
        var providedKey = httpContextAccessor.HttpContext?.Request.Headers[TriggerKeyHeader].ToString();

        return SharedKeyAuthorization.IsAuthorized(
            scrapingOptions.Value.SyncTriggerKey, providedKey, environment.IsDevelopment());
    }
}
