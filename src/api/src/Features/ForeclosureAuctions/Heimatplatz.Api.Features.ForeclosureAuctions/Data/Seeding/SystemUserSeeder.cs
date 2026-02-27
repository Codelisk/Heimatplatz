using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Heimatplatz.Api.Features.ForeclosureAuctions.Services;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Data.Seeding;

/// <summary>
/// Erstellt den System-User fuer automatisch generierte Properties aus Zwangsversteigerungen.
/// Wird vor UserSeeder (Order=5) ausgefuehrt, damit der System-User immer existiert.
/// </summary>
public class SystemUserSeeder(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher
) : ISeeder
{
    public int Order => 6;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var systemUserId = ForeclosureAuctionConstants.SystemUserId;

        if (await dbContext.Set<User>().AnyAsync(u => u.Id == systemUserId, cancellationToken))
            return;

        var systemUser = new User
        {
            Id = systemUserId,
            Vorname = "System",
            Nachname = "Heimatplatz",
            Email = "system@heimatplatz.at",
            PasswordHash = passwordHasher.Hash(Guid.NewGuid().ToString()),
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Set<User>().Add(systemUser);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
