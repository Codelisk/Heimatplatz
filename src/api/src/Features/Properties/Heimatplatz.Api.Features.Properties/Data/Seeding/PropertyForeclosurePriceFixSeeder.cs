using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.Properties.Data.Seeding;

/// <summary>
/// Korrigiert bestehende Demo-Zwangsversteigerungen, bei denen Price noch den
/// ursprünglichen Marktwert statt des Mindestgebots enthielt. Nur bekannte
/// Seed-Inserate werden verändert; echte Inserate und Produktionsdaten bleiben
/// unberührt, weil dieser Seeder Demo-Daten behandelt.
/// </summary>
public class PropertyForeclosurePriceFixSeeder(
    AppDbContext dbContext,
    ILogger<PropertyForeclosurePriceFixSeeder> logger) : ISeeder
{
    public int Order => 13;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var seedTitles = PropertyMunicipalityFixSeeder.SeedPropertyCities.Keys.ToList();
        var properties = await dbContext.Set<Property>()
            .Where(p => p.Type == PropertyType.Foreclosure && seedTitles.Contains(p.Title))
            .ToListAsync(cancellationToken);

        var hasChanges = false;
        foreach (var property in properties)
        {
            var data = property.GetTypedData<ForeclosurePropertyData>();
            if (data is not { MinimumBid: > 0 } || property.Price == data.MinimumBid)
                continue;

            logger.LogInformation(
                "[PropertyForeclosurePriceFix] '{Title}': Price von {OldPrice} auf Mindestgebot {MinimumBid} korrigiert",
                property.Title, property.Price, data.MinimumBid);
            property.Price = data.MinimumBid;
            hasChanges = true;
        }

        if (hasChanges)
            await dbContext.SaveChangesAsync(cancellationToken);
    }
}
