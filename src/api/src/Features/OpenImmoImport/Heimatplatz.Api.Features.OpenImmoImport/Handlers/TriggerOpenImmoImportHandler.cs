using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Features.OpenImmoImport.Configuration;
using Heimatplatz.Api.Features.OpenImmoImport.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.OpenImmoImport.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.OpenImmoImport.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/openimmo-import")]
public class TriggerOpenImmoImportHandler(
    IServiceScopeFactory scopeFactory,
    IHttpContextAccessor httpContextAccessor,
    IOptions<OpenImmoImportOptions> options,
    IHostEnvironment environment,
    ILogger<TriggerOpenImmoImportHandler> logger
) : IRequestHandler<TriggerOpenImmoImportRequest, TriggerOpenImmoImportResponse>
{
    public const string TriggerKeyHeader = "X-Sync-Key";

    // Shared-Key statt RequireAdmin (auf Prod gibt es keinen echten Admin-Account),
    // Defense-in-depth zusaetzlich zur Caddy-IP-Sperre auf /api/openimmo-import*
    // (deploy/hetzner/Caddyfile). Ohne konfigurierten Key ist der Endpoint ausserhalb
    // von Development gesperrt (fail-closed).
    [MediatorHttpPost("/sync", OperationId = "TriggerOpenImmoImport")]
    public Task<TriggerOpenImmoImportResponse> Handle(
        TriggerOpenImmoImportRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
            throw new UnauthorizedAccessException("Sync-Trigger-Key fehlt oder ist ungueltig.");

        if (string.IsNullOrWhiteSpace(options.Value.IncomingRootPath) || options.Value.Feeds.Count == 0)
        {
            return Task.FromResult(new TriggerOpenImmoImportResponse
            {
                Started = false,
                Message = "OpenImmo-Import ist nicht konfiguriert (IncomingRootPath/Feeds fehlen)"
            });
        }

        if (OpenImmoImportGuard.IsRunning)
        {
            return Task.FromResult(new TriggerOpenImmoImportResponse
            {
                Started = false,
                Message = "Import laeuft bereits - kein neuer Lauf gestartet"
            });
        }

        // Fire-and-forget: Bild-Downloads koennen dauern, der HTTP-Request soll sofort
        // antworten. Ergebnis steht danach im Status-Endpoint und im Log.
        var force = request.Force;
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var importService = scope.ServiceProvider.GetRequiredService<IOpenImmoImportService>();
                var results = await importService.TryRunAllFeedsAsync(force, CancellationToken.None);
                if (results == null)
                    return;

                foreach (var result in results)
                {
                    logger.LogInformation(
                        "[OpenImmoImport] Trigger-Ergebnis Feed {FeedKey}: {Outcome} {Summary}{Error}",
                        result.FeedKey, result.Outcome, result.Sync?.ToString() ?? "",
                        result.Error is null ? "" : $" ({result.Error})");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[OpenImmoImport] Manuell getriggerter Import fehlgeschlagen");
            }
        });

        return Task.FromResult(new TriggerOpenImmoImportResponse
        {
            Started = true,
            Message = "Import im Hintergrund gestartet"
        });
    }

    private bool IsAuthorized()
    {
        var providedKey = httpContextAccessor.HttpContext?.Request.Headers[TriggerKeyHeader].ToString();

        return SharedKeyAuthorization.IsAuthorized(
            options.Value.SyncTriggerKey, providedKey, environment.IsDevelopment());
    }
}
