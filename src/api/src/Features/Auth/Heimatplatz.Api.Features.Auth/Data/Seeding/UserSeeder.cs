using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Auth.Data.Seeding;

/// <summary>
/// Seeder fuer Testbenutzer
/// </summary>
public class UserSeeder(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher
) : ISeeder
{
    /// <summary>
    /// Reihenfolge: Benutzer sollten frueh geseedet werden
    /// </summary>
    public int Order => 5;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Idempotent: Nur seeden wenn keine Benutzer existieren
        if (await dbContext.Set<User>().AnyAsync(cancellationToken))
            return;

        var users = new List<User>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Vorname = "Max",
                Nachname = "Mustermann",
                Email = "max.mustermann@example.com",
                PasswordHash = passwordHasher.Hash("Test123!"),
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Vorname = "Anna",
                Nachname = "Schmidt",
                Email = "anna.schmidt@example.com",
                PasswordHash = passwordHasher.Hash("Test123!"),
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Vorname = "Thomas",
                Nachname = "Mueller",
                Email = "thomas.mueller@example.com",
                PasswordHash = passwordHasher.Hash("Test123!"),
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Vorname = "Lisa",
                Nachname = "Weber",
                Email = "lisa.weber@example.com",
                PasswordHash = passwordHasher.Hash("Test123!"),
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Vorname = "Admin",
                Nachname = "User",
                Email = "admin@heimatplatz.de",
                PasswordHash = passwordHasher.Hash("Admin123!"),
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        dbContext.Set<User>().AddRange(users);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
