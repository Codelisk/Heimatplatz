using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.ForeclosureAuctions.Data.Entities;
using Heimatplatz.Api.Features.ForeclosureAuctions.Services;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Data.Seeding;

/// <summary>
/// Einmaliger Backfill: Setzt Property.CreatedAt auf ForeclosureAuction.PublicationDate
/// fuer alle bestehenden Zwangsversteigerungs-Properties.
/// </summary>
public class ForeclosurePropertyCreatedAtBackfillSeeder(
    AppDbContext dbContext,
    ILogger<ForeclosurePropertyCreatedAtBackfillSeeder> logger) : ISeeder
{
    public int Order => 101;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var properties = await dbContext.Set<Property>()
            .Where(p => p.SourceName == ForeclosureAuctionConstants.SourceName && p.SourceId != null)
            .ToListAsync(cancellationToken);

        if (properties.Count == 0)
            return;

        var auctions = await dbContext.Set<ForeclosureAuction>()
            .Where(a => a.ExternalId != null)
            .ToDictionaryAsync(a => a.ExternalId!, cancellationToken);

        var updated = 0;
        foreach (var property in properties)
        {
            if (property.SourceId != null &&
                auctions.TryGetValue(property.SourceId, out var auction))
            {
                // Prefer PublicationDate, fallback to FirstSeenAt
                var targetDate = auction.PublicationDate ?? auction.FirstSeenAt;
                if (targetDate.HasValue && targetDate.Value != property.CreatedAt)
                {
                    property.CreatedAt = targetDate.Value;
                    updated++;
                }
            }
        }

        if (updated > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "[Backfill] Updated CreatedAt for {Count} foreclosure properties from PublicationDate/FirstSeenAt",
                updated);
        }
    }
}
