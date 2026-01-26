using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Properties.Data.Seeding;

/// <summary>
/// Seeds known seller sources (broker companies and portal platforms).
/// Order 5: Before PropertySeeder (10) so properties can reference these names.
/// </summary>
public class SellerSourceSeeder(AppDbContext dbContext) : ISeeder
{
    public int Order => 5;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Set<SellerSource>().AnyAsync(cancellationToken))
            return;

        var now = DateTimeOffset.UtcNow;

        var sources = new List<SellerSource>
        {
            // Portal sources
            new() { Id = Guid.NewGuid(), Name = "Willhaben", SellerType = SellerType.Portal, IsDefault = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "ImmoScout24", SellerType = SellerType.Portal, IsDefault = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "ImmobilienNET", SellerType = SellerType.Portal, IsDefault = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "FindMyHome", SellerType = SellerType.Portal, IsDefault = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "derStandard Immobilien", SellerType = SellerType.Portal, IsDefault = true, CreatedAt = now },

            // Broker sources
            new() { Id = Guid.NewGuid(), Name = "RE/MAX", SellerType = SellerType.Broker, IsDefault = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "ERA Immobilien", SellerType = SellerType.Broker, IsDefault = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "Engel & Voelkers", SellerType = SellerType.Broker, IsDefault = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "Raiffeisen Immobilien", SellerType = SellerType.Broker, IsDefault = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "sReal Immobilien", SellerType = SellerType.Broker, IsDefault = true, CreatedAt = now },
        };

        dbContext.Set<SellerSource>().AddRange(sources);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
