using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.Properties.Data.Seeding;

/// <summary>
/// Trägt Koordinaten auf Seed-Immobilien in bestehenden Datenbanken nach
/// (der PropertySeeder selbst läuft nur bei leerer Datenbank). Erfasst bewusst
/// nur die bekannten Seed-Objekte - echte Inserate (z.B. ZV-Importe) bekommen
/// ihre Koordinaten über das Geocoding beim Sync bzw. den Admin-Backfill
/// (POST /api/admin/properties/geocode).
/// </summary>
public class PropertyCoordinateBackfillSeeder(
    AppDbContext dbContext,
    ILogger<PropertyCoordinateBackfillSeeder> logger) : ISeeder
{
    // Nach PropertySeeder (10) und PropertyMunicipalityFixSeeder (11, legt Objekte nach)
    public int Order => 13;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var titles = PropertyMunicipalityFixSeeder.SeedPropertyCities.Keys.ToList();
        var seedProperties = await dbContext.Set<Property>()
            .Where(p => titles.Contains(p.Title) && p.Latitude == null)
            .ToListAsync(cancellationToken);

        if (seedProperties.Count == 0)
            return;

        var backfilled = 0;
        foreach (var property in seedProperties)
        {
            var cityName = PropertyMunicipalityFixSeeder.SeedPropertyCities[property.Title];
            PropertySeeder.ApplySeedCoordinates(property, cityName);
            if (property.Latitude != null)
                backfilled++;
        }

        if (backfilled > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("[PropertyCoordinateBackfill] {Count} Seed-Inserate mit Koordinaten versehen", backfilled);
        }
    }
}
