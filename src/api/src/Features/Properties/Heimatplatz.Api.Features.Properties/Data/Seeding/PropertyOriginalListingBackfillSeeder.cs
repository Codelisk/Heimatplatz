using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.Properties.Data.Seeding;

/// <summary>
/// Backfill für bestehende Datenbanken: Jedes Seed-Inserat bekommt am Erst-Kontakt einen
/// Original-Inserat-Beispiel-Link (intern auf /beispiel-originalinserat, kein echtes Portal).
/// Ältere PropertySeeder-Versionen setzten die URL nur bei ~30% der Objekte und verlinkten
/// dabei auf reale Fremdportale - diese URLs werden hier ebenfalls auf den anonymen
/// internen Beispiel-Link umgestellt. Der PropertySeeder selbst läuft nur bei leerer
/// Datenbank, daher braucht es diese idempotente Nachkorrektur.
/// Nutzer-erstellte Inserate werden nicht angefasst (Erkennung über die Seed-Titel).
/// </summary>
public class PropertyOriginalListingBackfillSeeder(
    AppDbContext dbContext,
    IConfiguration configuration,
    ILogger<PropertyOriginalListingBackfillSeeder> logger) : ISeeder
{
    // Nach PropertySeeder (10) und PropertyMunicipalityFixSeeder (11) ausführen,
    // damit auch nachgelegte Seed-Objekte erfasst sind
    public int Order => 12;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var titles = PropertyMunicipalityFixSeeder.SeedPropertyCities.Keys.ToList();
        var seedProperties = await dbContext.Set<Property>()
            .Include(p => p.Contacts)
            .Where(p => titles.Contains(p.Title))
            .ToListAsync(cancellationToken);

        var hasChanges = false;

        foreach (var property in seedProperties)
        {
            var (sourceName, url) = PropertySeeder.BuildOriginalListingLink(configuration, property.Title);

            // Alte Seed-Links auf reale Fremdportale durch den internen Beispiel-Link ersetzen
            foreach (var contact in property.Contacts.Where(c =>
                         !string.IsNullOrEmpty(c.OriginalListingUrl) && c.OriginalListingUrl != url))
            {
                logger.LogInformation(
                    "[PropertyOriginalListingBackfill] '{Title}': externe URL '{Old}' durch Beispiel-Link ersetzt",
                    property.Title, contact.OriginalListingUrl);
                contact.OriginalListingUrl = url;
                if (!string.IsNullOrEmpty(contact.SourceName))
                {
                    contact.SourceName = sourceName;
                    contact.SourceId = url[(url.LastIndexOf('=') + 1)..];
                }
                hasChanges = true;
            }

            // Erst-Kontakt ohne Link nachrüsten (Stand vor diesem Seeder: URL = null)
            var firstContact = property.Contacts.OrderBy(c => c.DisplayOrder).FirstOrDefault();
            if (firstContact != null && string.IsNullOrEmpty(firstContact.OriginalListingUrl))
            {
                logger.LogInformation(
                    "[PropertyOriginalListingBackfill] '{Title}': Beispiel-Link am Erst-Kontakt nachgerüstet",
                    property.Title);
                firstContact.OriginalListingUrl = url;
                hasChanges = true;
            }
        }

        if (hasChanges)
            await dbContext.SaveChangesAsync(cancellationToken);
    }
}
