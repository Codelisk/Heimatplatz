using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Auth.Data.Seeding;

/// <summary>
/// Seeder fuer Testbenutzer (neues Rollenmodell: Kaeufer implizit, Verkaeufer = SellerType gesetzt).
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

    // Debug-Test-User mit bekannten Credentials und festen IDs (fuer konsistente DB-Referenzen)
    public static readonly Guid DebugBuyerId = Guid.Parse("CC412C93-5D61-4AE2-B928-937812946ED2");
    public static readonly Guid DebugPrivateSellerId = Guid.Parse("3FFBDB4F-DC66-4FAC-97C0-23F6B525892B");
    public static readonly Guid DebugBrokerId = Guid.Parse("DF4E5296-0225-4E6E-8CDD-368F20E73704");
    public static readonly Guid DebugPropertyManagerId = Guid.Parse("A7B3C914-6E82-4D51-9F30-52C8E1D47A96");
    public static readonly Guid DebugAdminId = Guid.Parse("B92F4E71-3A05-4C68-8D14-7E96B0C25F83");

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Idempotent: Nur seeden wenn keine Benutzer existieren
        if (await dbContext.Set<User>().AnyAsync(cancellationToken))
            return;

        var now = DateTimeOffset.UtcNow;
        var testHash = passwordHasher.Hash("Test123!");

        var users = new List<User>
        {
            // Standard-Demo-User
            new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Max",
                LastName = "Mustermann",
                Email = "max.mustermann@example.com",
                PasswordHash = testHash,
                SellerType = SellerType.Private,
                CreatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Anna",
                LastName = "Schmidt",
                Email = "anna.schmidt@example.com",
                PasswordHash = testHash,
                CreatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Thomas",
                LastName = "Mueller",
                Email = "thomas.mueller@example.com",
                PasswordHash = testHash,
                CreatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Lisa",
                LastName = "Weber",
                Email = "lisa.weber@example.com",
                PasswordHash = testHash,
                SellerType = SellerType.Broker,
                CompanyName = "Weber Immobilien",
                CreatedAt = now
            },

            // Debug-User mit festen IDs
            new()
            {
                Id = DebugBuyerId,
                FirstName = "Test",
                LastName = "Buyer",
                Email = "test.buyer@heimatplatz.dev",
                PasswordHash = testHash,
                CreatedAt = now
            },
            new()
            {
                Id = DebugPrivateSellerId,
                FirstName = "Test",
                LastName = "Seller",
                Email = "test.seller@heimatplatz.dev",
                PasswordHash = testHash,
                SellerType = SellerType.Private,
                CreatedAt = now
            },
            new()
            {
                Id = DebugBrokerId,
                FirstName = "Test",
                LastName = "Broker",
                Email = "test.broker@heimatplatz.dev",
                PasswordHash = testHash,
                SellerType = SellerType.Broker,
                CompanyName = "Test Immobilien GmbH",
                CreatedAt = now
            },
            new()
            {
                Id = DebugPropertyManagerId,
                FirstName = "Test",
                LastName = "Verwaltung",
                Email = "test.verwaltung@heimatplatz.dev",
                PasswordHash = testHash,
                SellerType = SellerType.PropertyManager,
                CompanyName = "Test Hausverwaltung GmbH",
                CreatedAt = now
            },
            new()
            {
                Id = DebugAdminId,
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@heimatplatz.dev",
                PasswordHash = passwordHasher.Hash("Admin123!"),
                IsAdmin = true,
                CreatedAt = now
            }
        };

        // Seed-User gelten als bestaetigt - es haengt kein echtes Postfach dahinter
        foreach (var user in users)
        {
            user.EmailVerifiedAt = now;
        }

        dbContext.Set<User>().AddRange(users);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
