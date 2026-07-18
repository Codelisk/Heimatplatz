using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
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

        // Jeder Benutzer ist implizit Kaeufer - Demo-Favoriten fuer alle
        // Nicht-Admin-/Nicht-System-Konten anlegen
        var buyers = await dbContext.Set<User>()
            .Where(u => !u.IsAdmin && u.Email != "system@heimatplatz.at")
            .Select(u => u.Id)
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

        // Deterministischer Zufall: Store-Screenshots (Cake-Pipelines) sollen nach
        // jedem Test-DB-Reset dieselben Favoriten zeigen
        var random = new Random(20260718);

        // Jedem Buyer 3-5 Favoriten geben
        foreach (var buyerId in buyers)
        {
            var favoriteCount = random.Next(3, 6);

            // Properties auswaehlen (keine Duplikate)
            var selectedProperties = properties
                .OrderBy(_ => random.Next())
                .Take(favoriteCount)
                .ToList();

            foreach (var propertyId in selectedProperties)
            {
                favorites.Add(new Favorite
                {
                    Id = Guid.NewGuid(),
                    UserId = buyerId,
                    PropertyId = propertyId,
                    CreatedAt = now.AddDays(-random.Next(0, 30)) // Favoriten in letzten 30 Tagen erstellt
                });
            }
        }

        dbContext.Set<Favorite>().AddRange(favorites);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
