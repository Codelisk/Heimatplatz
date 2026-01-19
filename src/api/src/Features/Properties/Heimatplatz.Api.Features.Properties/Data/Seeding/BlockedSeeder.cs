using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Auth.Contracts.Enums;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Properties.Data.Seeding;

/// <summary>
/// Seeder fuer Test-Blockierungen
/// </summary>
public class BlockedSeeder(AppDbContext dbContext) : ISeeder
{
    /// <summary>
    /// Order 16: Nach Favorites (15), um bereits favorisierte Properties auszuschließen
    /// </summary>
    public int Order => 16;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Idempotent: Nur seeden wenn keine Blockierungen existieren
        if (await dbContext.Set<Blocked>().AnyAsync(cancellationToken))
            return;

        // Benutzer mit Buyer-Rolle abrufen
        var buyers = await dbContext.Set<UserRole>()
            .Where(ur => ur.RoleType == UserRoleType.Buyer)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (buyers.Count == 0)
        {
            // Keine Buyer vorhanden - Seeding ueberspringen
            return;
        }

        // Alle Properties abrufen
        var allProperties = await dbContext.Set<Property>()
            .OrderBy(p => p.Id)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (allProperties.Count == 0)
        {
            // Keine Properties vorhanden - Seeding ueberspringen
            return;
        }

        // Bereits favorisierte Properties pro Buyer abrufen
        var favoritesByBuyer = await dbContext.Set<Favorite>()
            .GroupBy(f => f.UserId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(f => f.PropertyId).ToHashSet(),
                cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var blockedList = new List<Blocked>();

        // Jedem Buyer 1-3 zufaellige Blockierungen geben (ohne bereits favorisierte)
        foreach (var buyerId in buyers)
        {
            // Properties holen, die der Buyer noch nicht favorisiert hat
            var favoritedPropertyIds = favoritesByBuyer.GetValueOrDefault(buyerId) ?? new HashSet<Guid>();
            var availableProperties = allProperties
                .Where(p => !favoritedPropertyIds.Contains(p))
                .ToList();

            if (availableProperties.Count == 0)
                continue;

            // Zufaellige Anzahl von Blockierungen (1-3)
            var blockedCount = Random.Shared.Next(1, Math.Min(4, availableProperties.Count + 1));

            // Zufaellige Properties auswaehlen (keine Duplikate)
            var selectedProperties = availableProperties
                .OrderBy(_ => Random.Shared.Next())
                .Take(blockedCount)
                .ToList();

            foreach (var propertyId in selectedProperties)
            {
                blockedList.Add(new Blocked
                {
                    Id = Guid.NewGuid(),
                    UserId = buyerId,
                    PropertyId = propertyId,
                    CreatedAt = now.AddDays(-Random.Shared.Next(0, 30)) // Blockierungen in letzten 30 Tagen erstellt
                });
            }
        }

        if (blockedList.Count > 0)
        {
            dbContext.Set<Blocked>().AddRange(blockedList);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
