using Heimatplatz.Api.Features.WkoCompanies.Configuration;
using Heimatplatz.Api.Features.WkoCompanies.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.WkoCompanies.Infrastructure;

/// <summary>
/// Hintergrund-Worker: ruft den WKO-Firmen-Sync periodisch auf (Konfiguration:
/// WkoCompanies:Scraping:SyncIntervalHours, Default 0 = deaktiviert). Analog zu
/// ForeclosureAuctionSyncWorker - der Sync laeuft standardmaessig nur manuell.
/// </summary>
public class WkoCompanySyncWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<WkoScrapingOptions> options,
    ILogger<WkoCompanySyncWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = options.Value.SyncIntervalHours;
        if (intervalHours <= 0)
        {
            logger.LogInformation(
                "[WkoSync] Automatischer Sync deaktiviert (SyncIntervalHours={IntervalHours})",
                intervalHours);
            return;
        }

        var interval = TimeSpan.FromHours(intervalHours);
        using var timer = new PeriodicTimer(interval);

        await RunSyncAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunSyncAsync(stoppingToken);
        }
    }

    private async Task RunSyncAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IWkoCompanySyncService>();
            var result = await syncService.SyncAllAsync(ct);

            logger.LogInformation(
                "[WkoSync] Automatischer Sync abgeschlossen: {Created} neu, {Updated} aktualisiert, " +
                "{Removed} entfernt, {Unchanged} unveraendert, {Errors} Fehler",
                result.Created, result.Updated, result.Removed, result.Unchanged, result.Errors);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutdown - kein Fehler
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[WkoSync] Automatischer Sync fehlgeschlagen");
        }
    }
}
