using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Properties.Data.Seeding;

/// <summary>
/// Seeder für Test-Favoriten
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

        // Jeder Benutzer ist implizit Käufer - Demo-Favoriten für alle
        // Nicht-Admin-/Nicht-System-Konten anlegen. OrderBy: ohne stabile Reihenfolge
        // wandert die Random-Sequenz pro Reset zwischen den Usern (GUIDs sind je Reset neu)
        var buyers = await dbContext.Set<User>()
            .Where(u => !u.IsAdmin && u.Email != "system@heimatplatz.at")
            .OrderBy(u => u.Email)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (buyers.Count == 0)
        {
            // Keine Buyer vorhanden - Seeding überspringen
            return;
        }

        // Alle Properties abrufen
        var properties = await dbContext.Set<Property>()
            .OrderBy(p => p.Id)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (properties.Count == 0)
        {
            // Keine Properties vorhanden - Seeding überspringen
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var favorites = new List<Favorite>();

        // Deterministischer Zufall: Store-Screenshots (Cake-Pipelines) sollen nach
        // jedem Test-DB-Reset dieselben Favoriten zeigen
        var random = new Random(20260718);

        // Screenshot-User der Store-Pipelines: kuratierte statt zufällige Favoriten -
        // die neuesten Häuser (Seed-Fotos!) ergeben ansprechende, stabile Store-Screenshots
        var screenshotUserId = Heimatplatz.Api.Features.Auth.Data.Seeding.UserSeeder.DebugBothId;
        if (buyers.Remove(screenshotUserId))
        {
            var curated = await dbContext.Set<Property>()
                .Where(p => p.Type == PropertyType.House)
                .OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
                .Take(5)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            for (var i = 0; i < curated.Count; i++)
            {
                favorites.Add(new Favorite
                {
                    Id = Guid.NewGuid(),
                    UserId = screenshotUserId,
                    PropertyId = curated[i],
                    CreatedAt = now.AddDays(-i)
                });
            }
        }

        // Jedem Buyer 3-5 Favoriten geben
        foreach (var buyerId in buyers)
        {
            var favoriteCount = random.Next(3, 6);

            // Properties auswählen (keine Duplikate)
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
