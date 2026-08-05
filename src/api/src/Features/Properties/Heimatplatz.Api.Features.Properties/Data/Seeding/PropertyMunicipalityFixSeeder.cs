using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Locations.Data.Entities;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Enums;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.Properties.Data.Seeding;

/// <summary>
/// Korrigiert Gemeinde-Zuordnungen der Seed-Immobilien in bestehenden Datenbanken.
/// Frühere PropertySeeder-Versionen hatten ein kaputtes Namens-Matching (kein Umlaut-Handling
/// plus stiller Fallback auf die erste Gemeinde) - dadurch hingen Objekte an falschen Gemeinden
/// und der Ort-Filter lieferte falsche Ergebnisse. Der PropertySeeder selbst läuft nur bei
/// leerer Datenbank, daher braucht es diese idempotente Nachkorrektur.
/// Legt außerdem später ergänzte Seed-Objekte nach (z.B. Roitham am Traunfall).
/// </summary>
public class PropertyMunicipalityFixSeeder(
    AppDbContext dbContext,
    Microsoft.Extensions.Configuration.IConfiguration configuration,
    ILogger<PropertyMunicipalityFixSeeder> logger) : ISeeder
{
    // Nach dem PropertySeeder ausführen
    public int Order => 11;

    /// <summary>
    /// Titel -> intendierte Gemeinde der Seed-Objekte (Stand PropertySeeder).
    /// Internal: dient auch anderen Backfill-Seedern als Kennung "das ist ein Seed-Objekt".
    /// </summary>
    internal static readonly Dictionary<string, string> SeedPropertyCities = new()
    {
        ["Einfamilienhaus in Linz-Urfahr"] = "Linz",
        ["Modernes Reihenhaus in Wels"] = "Wels",
        ["Villa am Traunsee"] = "Gmunden",
        ["Landhaus in Bad Ischl"] = "Bad Ischl",
        ["Familienhaus in Steyr"] = "Steyr",
        ["Baugrundstück in Wels"] = "Wels",
        ["Sonniges Baugrundstück Linz-Land"] = "Leonding",
        ["Großes Baugrundstück Mühlviertel"] = "Freistadt",
        ["Zwangsversteigerung: Haus in Traun"] = "Traun",
        ["Zwangsversteigerung: Grundstück Enns"] = "Enns",
        ["Bungalow in Braunau"] = "Braunau am Inn",
        ["Doppelhaushälfte Vöcklabruck"] = "Vöcklabruck",
        ["Einfamilienhaus am Traunfall"] = "Roitham am Traunfall"
    };

    private const string TraunfallTitle = "Einfamilienhaus am Traunfall";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var municipalityList = await dbContext.Set<Municipality>().ToListAsync(cancellationToken);
        if (municipalityList.Count == 0)
            return;

        var municipalities = municipalityList
            .GroupBy(m => MunicipalityNameResolver.NormalizeCityKey(m.Name))
            .ToDictionary(g => g.Key, g => g.First().Id);

        var titles = SeedPropertyCities.Keys.ToList();
        var seedProperties = await dbContext.Set<Property>()
            .Where(p => titles.Contains(p.Title))
            .ToListAsync(cancellationToken);

        var hasChanges = false;

        // Falsch zugeordnete Seed-Objekte auf die intendierte Gemeinde umhängen
        foreach (var property in seedProperties)
        {
            var expectedId = MunicipalityNameResolver.Resolve(municipalities, SeedPropertyCities[property.Title]);
            if (expectedId == null)
            {
                logger.LogWarning("[PropertyMunicipalityFix] Gemeinde '{City}' für '{Title}' nicht auflösbar",
                    SeedPropertyCities[property.Title], property.Title);
                continue;
            }

            if (property.MunicipalityId != expectedId.Value)
            {
                logger.LogInformation("[PropertyMunicipalityFix] '{Title}': Gemeinde korrigiert ({Old} -> {New})",
                    property.Title, property.MunicipalityId, expectedId.Value);
                property.MunicipalityId = expectedId.Value;
                hasChanges = true;
            }
        }

        // Roitham-Objekt nachlegen (ältere Datenbanken wurden vor diesem Seed-Eintrag befüllt)
        if (seedProperties.All(p => p.Title != TraunfallTitle))
        {
            var sellerId = await dbContext.Set<User>()
                .Where(u => u.SellerType != null)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var property = sellerId == Guid.Empty
                ? null
                : PropertySeeder.CreateProperty(TraunfallTitle, "Traunfallstraße 12", "Roitham am Traunfall", 365000,
                    140, 610, 5, 2012, PropertyType.House, SellerType.Private, "Familie Berger",
                    PropertySeedDescriptions.ByTitle[TraunfallTitle],
                    ["Garage", "Garten", "Terrasse", "Keller", "Kachelofen"],
                    PropertySeeder.BuildSeedImageUrls(configuration, "almhuette.jpg", "wiese-blumen.jpg", "interieur-schlafzimmer.jpg"),
                    city => MunicipalityNameResolver.Resolve(municipalities, city));

            if (property != null)
            {
                property.UserId = sellerId;
                property.CreatedAt = DateTimeOffset.UtcNow.AddDays(-2);

                PropertySeeder.SetTypeSpecificData([property]);
                PropertySeeder.SetContactData([property], configuration);

                dbContext.Set<Property>().Add(property);
                dbContext.Set<PropertyContactInfo>().AddRange(property.Contacts);
                hasChanges = true;

                logger.LogInformation("[PropertyMunicipalityFix] Seed-Objekt '{Title}' nachgelegt", TraunfallTitle);
            }
        }

        if (hasChanges)
            await dbContext.SaveChangesAsync(cancellationToken);
    }
}
