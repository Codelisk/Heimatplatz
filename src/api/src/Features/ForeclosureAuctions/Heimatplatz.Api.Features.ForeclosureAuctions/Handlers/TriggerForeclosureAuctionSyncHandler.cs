using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Features.ForeclosureAuctions.Configuration;
using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.ForeclosureAuctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/foreclosure-auctions")]
public class TriggerForeclosureAuctionSyncHandler(
    IServiceScopeFactory scopeFactory,
    IHttpContextAccessor httpContextAccessor,
    IOptions<ScrapingOptions> scrapingOptions,
    IHostEnvironment environment,
    ILogger<TriggerForeclosureAuctionSyncHandler> logger
) : IRequestHandler<TriggerForeclosureAuctionSyncRequest, TriggerForeclosureAuctionSyncResponse>
{
    public const string TriggerKeyHeader = "X-Sync-Key";

    // 0 = idle, 1 = Sync laeuft. Ohne diesen Guard startet jeder wiederholte Trigger einen
    // weiteren parallelen Scraper-Lauf gegen edikte.justiz.gv.at samt konkurrierender DB-Writes.
    private static int _syncRunning;

    // Shared-Key statt RequireAdmin, weil es auf Prod keinen echten Admin-Account gibt (nur der
    // Properties-System-User ohne Passwort/Rolle): Der Web-Container (/intern/sync.ts) schickt
    // SYNC_TRIGGER_KEY als X-Sync-Key-Header mit, die API vergleicht gegen
    // ForeclosureAuctions:Scraping:SyncTriggerKey. Defense-in-depth zusaetzlich zur Caddy-
    // IP-Sperre der Prod-API (deploy/hetzner/Caddyfile) - die Test-API hat keine IP-Sperre.
    // Ohne konfigurierten Key ist der Endpoint ausserhalb von Development gesperrt (fail-closed).
    [MediatorHttpPost("/sync", OperationId = "TriggerForeclosureAuctionSync")]
    public Task<TriggerForeclosureAuctionSyncResponse> Handle(
        TriggerForeclosureAuctionSyncRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
            throw new UnauthorizedAccessException("Sync-Trigger-Key fehlt oder ist ungueltig.");

        if (Interlocked.CompareExchange(ref _syncRunning, 1, 0) != 0)
        {
            logger.LogInformation("[Sync] Trigger ignoriert - Sync laeuft bereits");
            return Task.FromResult(new TriggerForeclosureAuctionSyncResponse
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
            finally
            {
                Interlocked.Exchange(ref _syncRunning, 0);
            }
        });

        return Task.FromResult(new TriggerForeclosureAuctionSyncResponse
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
