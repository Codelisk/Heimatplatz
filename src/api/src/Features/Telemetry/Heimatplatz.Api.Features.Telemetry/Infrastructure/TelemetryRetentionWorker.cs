using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Telemetry.Configuration;
using Heimatplatz.Api.Features.Telemetry.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.Telemetry.Infrastructure;

/// <summary>
/// Hintergrund-Worker: loescht Telemetrie-Eintraege aelter als die konfigurierte
/// Aufbewahrung (<see cref="TelemetryOptions.RetentionOptions"/>). Fehlergruppen
/// (<see cref="TelemetryErrorGroup"/>) bleiben dauerhaft erhalten.
/// </summary>
public class TelemetryRetentionWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<TelemetryOptions> options,
    ILogger<TelemetryRetentionWorker> logger
) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Beim Start kurz warten, damit Migrationen/Seeding zuerst durchlaufen
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var (removedLogs, removedSpans) =
                    await TrimOnceAsync(dbContext, options.Value, DateTimeOffset.UtcNow, stoppingToken);

                if (removedLogs > 0 || removedSpans > 0)
                {
                    logger.LogInformation(
                        "[Telemetry] Retention: {Logs} Logs und {Spans} Spans entfernt",
                        removedLogs,
                        removedSpans);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Telemetry] Retention fehlgeschlagen");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Ein Retention-Durchlauf; public als Test-Seam. Postgres loescht per
    /// ExecuteDelete, andere Provider ueber den ChangeTracker (InMemory-kompatibel;
    /// SQLite kann DateTimeOffset-Vergleiche nicht uebersetzen -&gt; in-memory filtern).
    /// </summary>
    public static async Task<(int RemovedLogs, int RemovedSpans)> TrimOnceAsync(
        AppDbContext dbContext,
        TelemetryOptions options,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var logCutoff = now.AddDays(-options.RetentionDays.Logs);
        var spanCutoff = now.AddDays(-options.RetentionDays.Spans);

        if (dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var removedLogs = await dbContext.Set<TelemetryLog>()
                .Where(l => l.TimestampUtc < logCutoff)
                .ExecuteDeleteAsync(ct);
            var removedSpans = await dbContext.Set<TelemetrySpan>()
                .Where(s => s.StartTimeUtc < spanCutoff)
                .ExecuteDeleteAsync(ct);
            return (removedLogs, removedSpans);
        }

        var expiredLogs = dbContext.Database.IsSqlite()
            ? (await dbContext.Set<TelemetryLog>().ToListAsync(ct)).Where(l => l.TimestampUtc < logCutoff).ToList()
            : await dbContext.Set<TelemetryLog>().Where(l => l.TimestampUtc < logCutoff).ToListAsync(ct);
        var expiredSpans = dbContext.Database.IsSqlite()
            ? (await dbContext.Set<TelemetrySpan>().ToListAsync(ct)).Where(s => s.StartTimeUtc < spanCutoff).ToList()
            : await dbContext.Set<TelemetrySpan>().Where(s => s.StartTimeUtc < spanCutoff).ToListAsync(ct);

        if (expiredLogs.Count == 0 && expiredSpans.Count == 0)
            return (0, 0);

        dbContext.RemoveRange(expiredLogs);
        dbContext.RemoveRange(expiredSpans);
        await dbContext.SaveChangesAsync(ct);

        return (expiredLogs.Count, expiredSpans.Count);
    }
}
