using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Locations.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Locations.Data.Entities;
using Heimatplatz.Api.Features.Locations.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Locations.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/locations")]
public class SeedLocationsHandler(
    AppDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    ILoggerFactory loggerFactory
) : IRequestHandler<SeedLocationsRequest, SeedLocationsResponse>
{
    [MediatorHttpPost("/seed", OperationId = "SeedLocations")]
    public async Task<SeedLocationsResponse> Handle(
        SeedLocationsRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if already seeded
            var existingCount = await dbContext.Set<FederalProvince>().CountAsync(cancellationToken);
            if (existingCount > 0)
            {
                var muniCount = await dbContext.Set<Municipality>().CountAsync(cancellationToken);
                return new SeedLocationsResponse(existingCount, 0, muniCount, "Already seeded");
            }

            // Run the seeder
            var seederLogger = loggerFactory.CreateLogger<LocationSeeder>();
            var seeder = new LocationSeeder(dbContext, httpClientFactory, seederLogger);
            await seeder.SeedAsync(cancellationToken);

            var provinces = await dbContext.Set<FederalProvince>().CountAsync(cancellationToken);
            var districts = await dbContext.Set<District>().CountAsync(cancellationToken);
            var municipalities = await dbContext.Set<Municipality>().CountAsync(cancellationToken);

            return new SeedLocationsResponse(provinces, districts, municipalities);
        }
        catch (Exception ex)
        {
            loggerFactory.CreateLogger<SeedLocationsHandler>().LogError(ex, "Error seeding locations");
            return new SeedLocationsResponse(0, 0, 0, $"{ex.GetType().Name}: {ex.Message}");
        }
    }
}
