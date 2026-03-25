using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Heimatplatz.Api.Features.SrealListings.Data.Entities;
using Heimatplatz.Api.Features.SrealListings.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.SrealListings.Data.Seeding;

/// <summary>
/// Einmaliger Backfill: Setzt Property.CreatedAt auf SrealListing.FirstSeenAt
/// fuer alle bestehenden SReal-Properties.
/// Laeuft nur einmal (prueft ob bereits angewendet).
/// </summary>
public class SrealPropertyCreatedAtBackfillSeeder(
    AppDbContext dbContext,
    ILogger<SrealPropertyCreatedAtBackfillSeeder> logger) : ISeeder
{
    public int Order => 100; // Nach allen anderen Seedern

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Alle SReal-Properties mit ihrem Listing joinen
        var srealProperties = await dbContext.Set<Property>()
            .Where(p => p.SourceName == SrealListingConstants.SourceName && p.SourceId != null)
            .ToListAsync(cancellationToken);

        if (srealProperties.Count == 0)
            return;

        var srealListings = await dbContext.Set<SrealListing>()
            .Where(l => l.FirstSeenAt != null)
            .ToDictionaryAsync(l => l.ExternalId, cancellationToken);

        var updated = 0;
        foreach (var property in srealProperties)
        {
            if (property.SourceId != null &&
                srealListings.TryGetValue(property.SourceId, out var listing) &&
                listing.FirstSeenAt.HasValue &&
                listing.FirstSeenAt.Value != property.CreatedAt)
            {
                property.CreatedAt = listing.FirstSeenAt.Value;
                updated++;
            }
        }

        if (updated > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("[Backfill] Updated CreatedAt for {Count} SReal properties from FirstSeenAt", updated);
        }
    }
}
