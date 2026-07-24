using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Features.Firmenbuch.Configuration;
using Heimatplatz.Api.Features.Firmenbuch.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Firmenbuch.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Firmenbuch.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/firmenbuch")]
public class TriggerFirmenbuchCatalogSyncHandler(
    IServiceScopeFactory scopeFactory,
    IHttpContextAccessor httpContextAccessor,
    IOptions<FirmenbuchHvdOptions> options,
    IHostEnvironment environment,
    ILogger<TriggerFirmenbuchCatalogSyncHandler> logger
) : IRequestHandler<TriggerFirmenbuchCatalogSyncRequest, TriggerFirmenbuchCatalogSyncResponse>
{
    public const string TriggerKeyHeader = "X-Sync-Key";

    // 0 = idle, 1 = Sync laeuft - verhindert parallele Katalog-Laeufe (konkurrierende
    // Upserts auf denselben FNRs + doppelte Last auf der Schnittstelle).
    private static int _syncRunning;

    [MediatorHttpPost("/catalog/sync", OperationId = "TriggerFirmenbuchCatalogSync")]
    public Task<TriggerFirmenbuchCatalogSyncResponse> Handle(
        TriggerFirmenbuchCatalogSyncRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
            throw new UnauthorizedAccessException("Sync-Trigger-Key fehlt oder ist ungueltig.");

        if (Interlocked.CompareExchange(ref _syncRunning, 1, 0) != 0)
        {
            logger.LogInformation("[FirmenbuchCatalog] Trigger ignoriert - Sync laeuft bereits");
            return Task.FromResult(new TriggerFirmenbuchCatalogSyncResponse
            {
                Messages = ["Sync laeuft bereits - kein neuer Lauf gestartet"]
            });
        }

        var ortNr = request.OrtNr?.Trim() ?? string.Empty;

        // Fire-and-forget wie beim Edikte-Sync: HTTP-Request kehrt sofort zurueck,
        // Fortschritt ist dank inkrementellem Speichern laufend ueber /catalog/status sichtbar.
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<IFirmenbuchCatalogSyncService>();
                var result = await syncService.SyncAsync(ortNr, CancellationToken.None);
                logger.LogInformation(
                    "[FirmenbuchCatalog] Background sync completed: {Created} created, {Updated} updated, {Unchanged} unchanged, {Queries} queries, {Capped} capped, {Errors} errors",
                    result.Created, result.Updated, result.Unchanged, result.QueriesExecuted, result.CappedPrefixes, result.Errors);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[FirmenbuchCatalog] Background sync failed");
            }
            finally
            {
                Interlocked.Exchange(ref _syncRunning, 0);
            }
        });

        return Task.FromResult(new TriggerFirmenbuchCatalogSyncResponse
        {
            Messages = [$"Sync started in background (OrtNr='{ortNr}')"]
        });
    }

    private bool IsAuthorized()
    {
        var providedKey = httpContextAccessor.HttpContext?.Request.Headers[TriggerKeyHeader].ToString();

        return SharedKeyAuthorization.IsAuthorized(
            options.Value.SyncTriggerKey, providedKey, environment.IsDevelopment());
    }
}
