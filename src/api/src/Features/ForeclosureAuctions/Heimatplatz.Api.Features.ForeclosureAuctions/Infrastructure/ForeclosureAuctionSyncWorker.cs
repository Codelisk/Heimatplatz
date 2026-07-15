using Heimatplatz.Api.Features.ForeclosureAuctions.Configuration;
using Heimatplatz.Api.Features.ForeclosureAuctions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Infrastructure;

/// <summary>
/// Hintergrund-Worker: ruft den Edikte-Sync periodisch auf (Konfiguration:
/// ForeclosureAuctions:Scraping:SyncIntervalHours, Default 6h). Vorher lief der Sync
/// ausschliesslich manuell ueber POST /api/foreclosure-auctions/sync - ohne diesen
/// Worker veralten die Zwangsversteigerungsdaten, sobald niemand mehr daran denkt,
/// den Endpoint aufzurufen.
/// </summary>
public class ForeclosureAuctionSyncWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ScrapingOptions> options,
    ILogger<ForeclosureAuctionSyncWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = options.Value.SyncIntervalHours;
        if (intervalHours <= 0)
        {
            logger.LogInformation(
                "[ForeclosureSync] Automatischer Sync deaktiviert (SyncIntervalHours={IntervalHours})",
                intervalHours);
            return;
        }

        var interval = TimeSpan.FromHours(intervalHours);
        using var timer = new PeriodicTimer(interval);

        // Erster Lauf kurz nach dem Start, nicht erst nach einem vollen Intervall -
        // ein frisch deployter Server soll nicht bis zu 6h auf die ersten Daten warten.
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
            var syncService = scope.ServiceProvider.GetRequiredService<IForeclosureAuctionSyncService>();
            var result = await syncService.SyncAllAsync(ct);

            logger.LogInformation(
                "[ForeclosureSync] Automatischer Sync abgeschlossen: {Created} neu, {Updated} aktualisiert, " +
                "{Removed} entfernt, {Unchanged} unveraendert, {Errors} Fehler",
                result.Created, result.Updated, result.Removed, result.Unchanged, result.Errors);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutdown - kein Fehler
        }
        catch (Exception ex)
        {
            // Ein fehlgeschlagener Lauf darf den Timer nicht stoppen - naechster Versuch
            // erfolgt beim naechsten Intervall-Tick.
            logger.LogError(ex, "[ForeclosureSync] Automatischer Sync fehlgeschlagen");
        }
    }
}
