using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts;
using Heimatplatz.Api.Features.SrealListings.Contracts;
using Heimatplatz.Api.Features.SrealListings.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.SrealListings.Data.Seeding;

public class SrealListingSeeder(AppDbContext dbContext) : ISeeder
{
    public int Order => 25;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Set<SrealListing>().AnyAsync(cancellationToken))
            return;

        var now = DateTimeOffset.UtcNow;

        dbContext.Set<SrealListing>().AddRange(
            new SrealListing
            {
                ExternalId = "964-31511",
                Title = "Wohnhaus in Altenfelden mit vielseitigen Nutzungsmoeglichkeiten",
                Address = "4121 Altenfelden",
                City = "Altenfelden",
                PostalCode = "4121",
                District = "Rohrbach",
                State = AustrianState.Oberoesterreich,
                ObjectType = SrealObjectType.House,
                BuyingType = "buy",
                Price = 460000m,
                PriceText = "460.000,00 €",
                Commission = "3% des Kaufpreises zzgl. 20% USt.",
                LivingArea = 240m,
                PlotArea = 958m,
                EnergyClass = "E",
                EnergyValue = "162,9 kWh/m²a",
                FGee = "2.16",
                FGeeClass = "D",
                SourceUrl = "https://www.sreal.at/de/immobilie/964-31511/wohnhaus-in-altenfelden",
                AgentName = "Herr Simon Hintringer",
                AgentPhone = "+43 (5) 0100 - 42840",
                AgentEmail = "simon.hintringer@sreal.at",
                AgentOffice = "s REAL Rohrbach",
                ContentHash = "seed-001",
                IsActive = true,
                FirstSeenAt = now,
                LastScrapedAt = now
            },
            new SrealListing
            {
                ExternalId = "964-32100",
                Title = "Einfamilienhaus in Gramastetten mit grossem Garten",
                Address = "4201 Gramastetten",
                City = "Gramastetten",
                PostalCode = "4201",
                District = "Urfahr-Umgebung",
                State = AustrianState.Oberoesterreich,
                ObjectType = SrealObjectType.House,
                BuyingType = "buy",
                Price = 389000m,
                PriceText = "389.000,00 €",
                Commission = "3% des Kaufpreises zzgl. 20% USt.",
                LivingArea = 150m,
                PlotArea = 750m,
                Rooms = 5,
                EnergyClass = "C",
                EnergyValue = "95,3 kWh/m²a",
                SourceUrl = "https://www.sreal.at/de/immobilie/964-32100/einfamilienhaus-gramastetten",
                AgentName = "Frau Maria Huber",
                AgentPhone = "+43 (5) 0100 - 42850",
                AgentEmail = "maria.huber@sreal.at",
                AgentOffice = "s REAL Linz",
                ContentHash = "seed-002",
                IsActive = true,
                FirstSeenAt = now,
                LastScrapedAt = now
            },
            new SrealListing
            {
                ExternalId = "964-32200",
                Title = "Sonniges Baugrundstück in Bad Leonfelden",
                Address = "4190 Bad Leonfelden",
                City = "Bad Leonfelden",
                PostalCode = "4190",
                District = "Urfahr-Umgebung",
                State = AustrianState.Oberoesterreich,
                ObjectType = SrealObjectType.Land,
                BuyingType = "buy",
                Price = 85000m,
                PriceText = "85.000,00 €",
                PlotArea = 1239m,
                SourceUrl = "https://www.sreal.at/de/immobilie/964-32200/baugrundstück-bad-leonfelden",
                AgentName = "Herr Thomas Mayr",
                AgentPhone = "+43 (5) 0100 - 42830",
                AgentEmail = "thomas.mayr@sreal.at",
                AgentOffice = "s REAL Freistadt",
                ContentHash = "seed-003",
                IsActive = true,
                FirstSeenAt = now,
                LastScrapedAt = now
            },
            new SrealListing
            {
                ExternalId = "964-32300",
                Title = "Charmantes Ferienhaus am Attersee",
                Address = "4864 Attersee",
                City = "Attersee",
                PostalCode = "4864",
                District = "Vöcklabruck",
                State = AustrianState.Oberoesterreich,
                ObjectType = SrealObjectType.Vacation,
                BuyingType = "buy",
                Price = 750000m,
                PriceText = "750.000,00 €",
                Commission = "3% des Kaufpreises zzgl. 20% USt.",
                LivingArea = 120m,
                PlotArea = 500m,
                Rooms = 4,
                EnergyClass = "D",
                EnergyValue = "130,5 kWh/m²a",
                SourceUrl = "https://www.sreal.at/de/immobilie/964-32300/ferienhaus-attersee",
                AgentName = "Frau Lisa Berger",
                AgentPhone = "+43 (5) 0100 - 42860",
                AgentEmail = "lisa.berger@sreal.at",
                AgentOffice = "s REAL Vöcklabruck",
                ContentHash = "seed-004",
                IsActive = true,
                FirstSeenAt = now,
                LastScrapedAt = now
            },
            new SrealListing
            {
                ExternalId = "964-32400",
                Title = "Grosszuegiges Landhaus in Schärding",
                Address = "4780 Schärding",
                City = "Schärding",
                PostalCode = "4780",
                District = "Schärding",
                State = AustrianState.Oberoesterreich,
                ObjectType = SrealObjectType.House,
                BuyingType = "buy",
                Price = 295000m,
                PriceText = "295.000,00 €",
                LivingArea = 180m,
                PlotArea = 1500m,
                Rooms = 6,
                SourceUrl = "https://www.sreal.at/de/immobilie/964-32400/landhaus-schaerding",
                AgentName = "Herr Markus Gruber",
                AgentEmail = "markus.gruber@sreal.at",
                AgentOffice = "s REAL Schärding",
                ContentHash = "seed-005",
                IsActive = true,
                FirstSeenAt = now,
                LastScrapedAt = now
            },
            new SrealListing
            {
                ExternalId = "964-32500",
                Title = "Idyllisches Grundstück in Mondsee",
                Address = "5310 Mondsee",
                City = "Mondsee",
                PostalCode = "5310",
                District = "Vöcklabruck",
                State = AustrianState.Oberoesterreich,
                ObjectType = SrealObjectType.Land,
                BuyingType = "buy",
                Price = 195000m,
                PriceText = "195.000,00 €",
                PlotArea = 800m,
                SourceUrl = "https://www.sreal.at/de/immobilie/964-32500/grundstueck-mondsee",
                AgentName = "Frau Lisa Berger",
                AgentEmail = "lisa.berger@sreal.at",
                AgentOffice = "s REAL Vöcklabruck",
                ContentHash = "seed-006",
                IsActive = true,
                FirstSeenAt = now,
                LastScrapedAt = now
            },
            new SrealListing
            {
                ExternalId = "964-32600",
                Title = "Renoviertes Bauernhaus in Pregarten",
                Address = "4230 Pregarten",
                City = "Pregarten",
                PostalCode = "4230",
                District = "Freistadt",
                State = AustrianState.Oberoesterreich,
                ObjectType = SrealObjectType.House,
                BuyingType = "buy",
                Price = 320000m,
                PriceText = "320.000,00 €",
                Commission = "3% des Kaufpreises zzgl. 20% USt.",
                LivingArea = 200m,
                PlotArea = 2000m,
                Rooms = 7,
                EnergyClass = "D",
                SourceUrl = "https://www.sreal.at/de/immobilie/964-32600/bauernhaus-pregarten",
                ContentHash = "seed-007",
                IsActive = true,
                FirstSeenAt = now,
                LastScrapedAt = now
            }
        );

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
