using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.Properties.Data.Seeding;

/// <summary>
/// Zieht die Beschreibungen der Seed-Immobilien in Bestands-Datenbanken auf den
/// Stand von PropertySeedDescriptions nach. Der PropertySeeder selbst läuft nur
/// bei leerer Datenbank - Textänderungen (z.B. die langen Falz-Testtexte vom
/// August 2026) kämen ohne diese Nachkorrektur nie in bereits geseedete
/// Umgebungen. Idempotent: schreibt nur, wenn der Text tatsächlich abweicht;
/// das Delta-Sync-Journal wird über den PropertyChangeInterceptor automatisch
/// mitgeführt.
/// </summary>
public class PropertyDescriptionRefreshSeeder(
    AppDbContext dbContext,
    ILogger<PropertyDescriptionRefreshSeeder> logger) : ISeeder
{
    // Nach den übrigen Seed-Fix-Seedern (MunicipalityFix legt ggf. Objekte nach)
    public int Order => 14;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var titles = PropertySeedDescriptions.ByTitle.Keys.ToList();

        // Nur echte Seed-Objekte anfassen: Titel-Match reicht nicht, ein Nutzer
        // könnte ein gleichnamiges Inserat angelegt haben - die Seed-Kennung ist
        // Titel + intendierte Seed-Gemeinde (wie bei den anderen Fix-Seedern).
        var candidates = await dbContext.Set<Property>()
            .Where(p => titles.Contains(p.Title))
            .ToListAsync(cancellationToken);

        var updated = 0;
        foreach (var property in candidates)
        {
            if (!PropertyMunicipalityFixSeeder.SeedPropertyCities.ContainsKey(property.Title))
                continue;

            var expected = PropertySeedDescriptions.ByTitle[property.Title];
            if (property.Description == expected)
                continue;

            property.Description = expected;
            updated++;
            logger.LogInformation("[PropertyDescriptionRefresh] '{Title}': Beschreibung aktualisiert ({Length} Zeichen)",
                property.Title, expected.Length);
        }

        if (updated > 0)
            await dbContext.SaveChangesAsync(cancellationToken);
    }
}
