using Heimatplatz.Api.Features.OpenImmoImport.Configuration;
using Heimatplatz.Api.Features.OpenImmoImport.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.OpenImmoImport.Infrastructure;

/// <summary>
/// Hintergrund-Worker: scannt die FTP-Drop-Ordner periodisch auf neue Feed-Dateien
/// (Konfiguration: OpenImmoImport:ScanIntervalMinutes, Default 0 = deaktiviert -
/// Integrationstests booten alle Hosted Services). Der Marker-Kurzschluss macht
/// haeufige Scans billig: unveraenderte Dateien kosten nur einen Verzeichnis-Stat.
/// </summary>
public class OpenImmoImportWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OpenImmoImportOptions> options,
    ILogger<OpenImmoImportWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = options.Value.ScanIntervalMinutes;
        if (intervalMinutes <= 0 || string.IsNullOrWhiteSpace(options.Value.IncomingRootPath))
        {
            logger.LogInformation(
                "[OpenImmoImport] Automatischer Scan deaktiviert (ScanIntervalMinutes={IntervalMinutes}, IncomingRootPath={IncomingRootPath})",
                intervalMinutes, options.Value.IncomingRootPath);
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        // Erster Lauf direkt nach dem Start - ein frisch deployter Server soll eine
        // bereits liegende Feed-Datei nicht erst nach einem vollen Intervall einlesen
        await RunScanAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunScanAsync(stoppingToken);
        }
    }

    private async Task RunScanAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<IOpenImmoImportService>();
            var results = await importService.TryRunAllFeedsAsync(force: false, ct);
            if (results == null)
                return; // Lauf uebersprungen - Import laeuft bereits (z.B. manueller Trigger)

            foreach (var result in results.Where(r => r.Outcome
                         is OpenImmoFeedRunOutcome.Imported or OpenImmoFeedRunOutcome.Failed))
            {
                logger.LogInformation(
                    "[OpenImmoImport] Scan-Ergebnis Feed {FeedKey}: {Outcome} {Summary}{Error}",
                    result.FeedKey, result.Outcome, result.Sync?.ToString() ?? "",
                    result.Error is null ? "" : $" ({result.Error})");
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutdown - kein Fehler
        }
        catch (Exception ex)
        {
            // Ein fehlgeschlagener Lauf darf den Timer nicht stoppen - naechster
            // Versuch beim naechsten Intervall-Tick
            logger.LogError(ex, "[OpenImmoImport] Automatischer Scan fehlgeschlagen");
        }
    }
}
