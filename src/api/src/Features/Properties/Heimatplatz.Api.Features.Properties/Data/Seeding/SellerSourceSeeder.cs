using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Properties.Data.Seeding;

/// <summary>
/// Seeds known seller sources (broker companies).
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
            // Broker sources
            new() { Id = Guid.NewGuid(), Name = "RE/MAX", IsDefault = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "ERA Immobilien", IsDefault = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "Engel & Voelkers", IsDefault = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "Raiffeisen Immobilien", IsDefault = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "sReal Immobilien", IsDefault = true, CreatedAt = now },
        };

        dbContext.Set<SellerSource>().AddRange(sources);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
