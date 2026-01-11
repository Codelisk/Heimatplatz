using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Core.Data.Seeding;

/// <summary>
/// Runs all registered seeders in order.
/// </summary>
public class SeederRunner(IServiceProvider serviceProvider, ILogger<SeederRunner> logger)
{
    public async Task RunAllSeedersAsync(CancellationToken cancellationToken = default)
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

        logger.LogInformation("Running {Count} seeders", seeders.Count);

        foreach (var seeder in seeders)
        {
            var seederName = seeder.GetType().Name;
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
