using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Heimatplatz.Api.Features.Properties.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.Properties.Infrastructure;

/// <summary>
/// Hintergrund-Worker: loescht PropertyChange-Journal-Eintraege, die aelter als die
/// Aufbewahrungsfrist sind (<see cref="GetPropertyChangesHandler.RetentionPeriod"/>).
/// Clients mit aelterem Sync-Stand erhalten vom Changes-Endpoint FullResyncRequired.
/// </summary>
public class PropertyChangeRetentionWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<PropertyChangeRetentionWorker> logger
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
                await TrimAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Properties] Journal-Bereinigung fehlgeschlagen");
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

    private async Task TrimAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoff = DateTimeOffset.UtcNow - GetPropertyChangesHandler.RetentionPeriod;

        // Bewusst ueber den ChangeTracker statt ExecuteDelete (ueberschaubare Mengen);
        // SQLite kann DateTimeOffset-Vergleiche nicht uebersetzen -> in-memory filtern
        var journal = dbContext.Set<PropertyChange>();
        var expired = GetPropertyChangesHandler.IsSqlite(dbContext)
            ? (await journal.ToListAsync(ct)).Where(c => c.CreatedAt < cutoff).ToList()
            : await journal.Where(c => c.CreatedAt < cutoff).ToListAsync(ct);

        if (expired.Count == 0)
            return;

        dbContext.RemoveRange(expired);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "[Properties] {Count} Journal-Eintraege aelter als {Cutoff:u} entfernt",
            expired.Count,
            cutoff);
    }
}
