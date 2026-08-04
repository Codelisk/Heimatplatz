using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Partners.Contracts.Models;
using Heimatplatz.Api.Features.Partners.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Partners.Data.Seeding;

/// <summary>
/// Demo-Partner fuer Entwicklung und Testumgebung (IsDemoData=true, laeuft nie in
/// Produktion). Echte Partner werden ueber /intern/partner/ gepflegt - fiktive Namen,
/// damit Testdaten nicht wie echte Zusagen aussehen.
/// </summary>
public class PartnersDemoSeeder(AppDbContext dbContext) : ISeeder
{
    public int Order => 40;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Idempotent: Nur seeden wenn leer
        if (await dbContext.Set<Partner>().AnyAsync(cancellationToken))
            return;

        dbContext.Set<Partner>().AddRange(
            new Partner
            {
                Name = "Mustermann Immobilien",
                Category = PartnerCategories.Broker,
                Description = "Familienbetrieb in zweiter Generation mit Schwerpunkt auf Einfamilienhäuser im Salzkammergut. Persönliche Betreuung von der Besichtigung bis zur Übergabe.",
                WebsiteUrl = "https://www.example.com/mustermann",
                Region = "Salzkammergut, Oberösterreich",
                PartnerSinceYear = 2026,
                DisplayOrder = 10,
                IsVisible = true,
            },
            new Partner
            {
                Name = "Traunblick Immobilien",
                Category = PartnerCategories.Broker,
                Description = "Regionales Maklerbüro rund um den Traunsee mit Fokus auf Häuser und Seegrundstücke.",
                WebsiteUrl = "https://www.example.com/traunblick",
                Region = "Gmunden, Oberösterreich",
                PartnerSinceYear = 2026,
                DisplayOrder = 20,
                IsVisible = true,
            },
            new Partner
            {
                Name = "Innviertler Grund & Boden",
                Category = PartnerCategories.Broker,
                Description = "Spezialisiert auf landwirtschaftliche Flächen und Baugrundstücke im Innviertel.",
                WebsiteUrl = "https://www.example.com/innviertler",
                Region = "Ried im Innkreis, Oberösterreich",
                PartnerSinceYear = 2026,
                DisplayOrder = 30,
                IsVisible = true,
            },
            new Partner
            {
                Name = "Mühlviertler Wohnwerkstatt",
                Category = PartnerCategories.Broker,
                Description = "Junges Team mit Fokus auf Sanierungsobjekte und leistbares Wohnen im Mühlviertel.",
                WebsiteUrl = "https://www.example.com/wohnwerkstatt",
                Region = "Freistadt, Oberösterreich",
                PartnerSinceYear = 2026,
                DisplayOrder = 40,
                IsVisible = true,
            },
            new Partner
            {
                Name = "Zentralraum Immobilien Beispiel GmbH",
                Category = PartnerCategories.Broker,
                Description = "Ausgeblendeter Demo-Eintrag: prueft in der Pflege-UI den Sichtbarkeits-Schalter.",
                Region = "Linz-Land, Oberösterreich",
                PartnerSinceYear = 2026,
                DisplayOrder = 50,
                IsVisible = false,
            }
        );

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
