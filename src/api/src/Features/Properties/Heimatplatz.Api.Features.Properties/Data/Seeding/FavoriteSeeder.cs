using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Auth.Contracts.Enums;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Properties.Data.Seeding;

/// <summary>
/// Seeder fuer Test-Favoriten
/// </summary>
public class FavoriteSeeder(AppDbContext dbContext) : ISeeder
{
    /// <summary>
    /// Order 15: Nach Properties (10) und Users (5)
    /// </summary>
    public int Order => 15;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Idempotent: Nur seeden wenn keine Favoriten existieren
        if (await dbContext.Set<Favorite>().AnyAsync(cancellationToken))
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
        var properties = await dbContext.Set<Property>()
            .OrderBy(p => p.Id)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (properties.Count == 0)
        {
            // Keine Properties vorhanden - Seeding ueberspringen
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var favorites = new List<Favorite>();

        // Jedem Buyer 3-5 zufaellige Favoriten geben
        foreach (var buyerId in buyers)
        {
            // Zufaellige Anzahl von Favoriten (3-5)
            var favoriteCount = Random.Shared.Next(3, 6);

            // Zufaellige Properties auswaehlen (keine Duplikate)
            var selectedProperties = properties
                .OrderBy(_ => Random.Shared.Next())
                .Take(favoriteCount)
                .ToList();

            foreach (var propertyId in selectedProperties)
            {
                favorites.Add(new Favorite
                {
                    Id = Guid.NewGuid(),
                    UserId = buyerId,
                    PropertyId = propertyId,
                    CreatedAt = now.AddDays(-Random.Shared.Next(0, 30)) // Favoriten in letzten 30 Tagen erstellt
                });
            }
        }

        dbContext.Set<Favorite>().AddRange(favorites);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
