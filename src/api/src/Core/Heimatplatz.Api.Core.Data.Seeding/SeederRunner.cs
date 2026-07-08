using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Core.Data.Seeding;

/// <summary>
/// Runs all registered seeders in order.
/// </summary>
public class SeederRunner(IServiceProvider serviceProvider, ILogger<SeederRunner> logger)
{
    /// <summary>
    /// Runs all registered seeders. Demo data seeders (IsDemoData=true) are skipped
    /// unless <paramref name="includeDemoData"/> is true - essential reference-data
    /// seeders always run.
    /// </summary>
    public async Task RunAllSeedersAsync(bool includeDemoData, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var seeders = scope.ServiceProvider.GetServices<ISeeder>()
            .OrderBy(s => s.Order)
            .ToList();

        if (seeders.Count == 0)
        {
            logger.LogInformation("No seeders registered");
            return;
        }

        logger.LogInformation("Running {Count} seeders (includeDemoData={IncludeDemoData})", seeders.Count, includeDemoData);

        foreach (var seeder in seeders)
        {
            var seederName = seeder.GetType().Name;

            if (seeder.IsDemoData && !includeDemoData)
            {
                logger.LogInformation("Skipping demo data seeder: {SeederName} (seeding disabled)", seederName);
                continue;
            }

            logger.LogInformation("Running seeder: {SeederName}", seederName);

            try
            {
                await seeder.SeedAsync(cancellationToken);
                logger.LogInformation("Seeder {SeederName} completed", seederName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Seeder {SeederName} failed", seederName);
                throw;
            }
        }

        logger.LogInformation("All seeders completed");
    }
}
