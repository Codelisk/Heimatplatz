using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Auth.Contracts.Enums;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific.Enums;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Properties.Data.Seeding;

/// <summary>
/// Seeder fuer Beispiel-Immobilien in Oberoesterreich
/// </summary>
public class PropertySeeder(AppDbContext dbContext) : ISeeder
{
    public int Order => 10;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Set<Property>().AnyAsync(cancellationToken))
            return;

        // Benutzer mit Seller-Rolle abrufen
        var sellers = await dbContext.Set<UserRole>()
            .Where(ur => ur.RoleType == UserRoleType.Seller)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (sellers.Count == 0)
        {
            // Keine Seller vorhanden - Seeding ueberspringen
            return;
        }

        // Properties gleichmaessig auf Seller verteilen
        var sellerIndex = 0;
        Guid GetNextSellerId()
        {
            var sellerId = sellers[sellerIndex];
            sellerIndex = (sellerIndex + 1) % sellers.Count;
            return sellerId;
        }

        var now = DateTimeOffset.UtcNow;
        var properties = new List<Property>
        {
            // Haeuser
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Einfamilienhaus in Linz-Urfahr",
                Address = "Hauptstrasse 15",
                City = "Linz",
                PostalCode = "4040",
                Price = 349000,
                LivingAreaSquareMeters = 145,
                PlotAreaSquareMeters = 520,
                Rooms = 5,
                YearBuilt = 2018,
                Type = PropertyType.House,
                SellerType = SellerType.Makler,
                SellerName = "Mustermann Immobilien",
                Description = "Wunderschoenes Einfamilienhaus mit grossem Garten in ruhiger Lage. Hochwertige Ausstattung, Fussbodenheizung, Photovoltaikanlage.",
                Features = ["Garage", "Garten", "Terrasse", "Keller", "Fussbodenheizung", "Photovoltaik"],
                ImageUrls = ["https://picsum.photos/seed/haus1/800/600"]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Modernes Reihenhaus in Wels",
                Address = "Ringstrasse 42",
                City = "Wels",
                PostalCode = "4600",
                Price = 289000,
                LivingAreaSquareMeters = 120,
                PlotAreaSquareMeters = 180,
                Rooms = 4,
                YearBuilt = 2020,
                Type = PropertyType.House,
                SellerType = SellerType.Privat,
                SellerName = "Familie Huber",
                Description = "Neuwertiges Reihenhaus in zentraler Lage. Perfekt fuer junge Familien. Kurze Wege zu Schulen und Geschaeften.",
                Features = ["Carport", "Terrasse", "Keller", "Fussbodenheizung"],
                ImageUrls = ["https://picsum.photos/seed/haus2/800/600"]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Villa am Traunsee",
                Address = "Seeuferweg 8",
                City = "Gmunden",
                PostalCode = "4810",
                Price = 890000,
                LivingAreaSquareMeters = 220,
                PlotAreaSquareMeters = 1200,
                Rooms = 7,
                YearBuilt = 2015,
                Type = PropertyType.House,
                SellerType = SellerType.Makler,
                SellerName = "Luxus Immobilien GmbH",
                Description = "Exklusive Villa mit direktem Seezugang. Panoramablick auf den Traunsee. Hochwertigste Ausstattung.",
                Features = ["Doppelgarage", "Pool", "Sauna", "Seezugang", "Smarthome", "Klimaanlage"],
                ImageUrls = ["https://picsum.photos/seed/villa1/800/600"]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Landhaus in Bad Ischl",
                Address = "Kaiserweg 23",
                City = "Bad Ischl",
                PostalCode = "4820",
                Price = 425000,
                LivingAreaSquareMeters = 165,
                PlotAreaSquareMeters = 850,
                Rooms = 5,
                YearBuilt = 1998,
                Type = PropertyType.House,
                SellerType = SellerType.Privat,
                SellerName = "Herr Maier",
                Description = "Charmantes Landhaus im Salzkammergut. Renoviert mit Liebe zum Detail. Idealer Rueckzugsort.",
                Features = ["Garage", "Garten", "Kachelofen", "Keller", "Dachboden"],
                ImageUrls = ["https://picsum.photos/seed/landhaus1/800/600"]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Familienhaus in Steyr",
                Address = "Bahnhofstrasse 67",
                City = "Steyr",
                PostalCode = "4400",
                Price = 315000,
                LivingAreaSquareMeters = 135,
                PlotAreaSquareMeters = 450,
                Rooms = 5,
                YearBuilt = 2010,
                Type = PropertyType.House,
                SellerType = SellerType.Makler,
                SellerName = "Immobilien Steyr",
                Description = "Gepflegtes Einfamilienhaus in guter Lage. Nahe Stadtzentrum und Naturgebiet.",
                Features = ["Garage", "Garten", "Terrasse", "Keller"],
                ImageUrls = ["https://picsum.photos/seed/haus3/800/600"]
            },

            // Grundstuecke
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Baugrundstück in Wels",
                Address = "Neubaugebiet Sued",
                City = "Wels",
                PostalCode = "4600",
                Price = 189000,
                PlotAreaSquareMeters = 850,
                Type = PropertyType.Land,
                SellerType = SellerType.Privat,
                SellerName = "Familie Mueller",
                Description = "Voll erschlossenes Baugrundstuck in ruhiger Wohnlage. Alle Anschluesse vorhanden.",
                Features = ["Erschlossen", "Strom", "Wasser", "Kanal", "Gas"],
                ImageUrls = ["https://picsum.photos/seed/grund1/800/600"]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Sonniges Baugrundstück Linz-Land",
                Address = "Am Sonnenhang 12",
                City = "Leonding",
                PostalCode = "4060",
                Price = 245000,
                PlotAreaSquareMeters = 720,
                Type = PropertyType.Land,
                SellerType = SellerType.Makler,
                SellerName = "Grund & Boden OOe",
                Description = "Suedhanglage mit herrlichem Ausblick. Bebauungsplan liegt vor.",
                Features = ["Erschlossen", "Suedlage", "Aussicht"],
                ImageUrls = ["https://picsum.photos/seed/grund2/800/600"]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Grosses Baugrundstück Muehlviertel",
                Address = "Dorfstrasse",
                City = "Freistadt",
                PostalCode = "4240",
                Price = 95000,
                PlotAreaSquareMeters = 1200,
                Type = PropertyType.Land,
                SellerType = SellerType.Privat,
                SellerName = "Gemeinde Freistadt",
                Description = "Guenstiges Baugrundstuck im schoenen Muehlviertel. Ruhige Lage, gute Infrastruktur.",
                Features = ["Teilerschlossen", "Strom", "Wasser"],
                ImageUrls = ["https://picsum.photos/seed/grund3/800/600"]
            },

            // Zwangsversteigerungen
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Zwangsversteigerung: Haus in Traun",
                Address = "Industriestrasse 45",
                City = "Traun",
                PostalCode = "4050",
                Price = 185000,
                LivingAreaSquareMeters = 110,
                PlotAreaSquareMeters = 380,
                Rooms = 4,
                YearBuilt = 1985,
                Type = PropertyType.Foreclosure,
                SellerType = SellerType.Makler,
                SellerName = "Bezirksgericht Linz",
                Description = "Aelteres Haus mit Renovierungsbedarf. Versteigerungstermin: naechsten Monat. Besichtigung moeglich.",
                Features = ["Garage", "Keller"],
                ImageUrls = ["https://picsum.photos/seed/zwang1/800/600"]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Zwangsversteigerung: Grundstück Enns",
                Address = "Feldweg 3",
                City = "Enns",
                PostalCode = "4470",
                Price = 68000,
                PlotAreaSquareMeters = 650,
                Type = PropertyType.Foreclosure,
                SellerType = SellerType.Makler,
                SellerName = "Bezirksgericht Steyr",
                Description = "Baugrundstuck aus Zwangsversteigerung. Gute Lage, erschlossen.",
                Features = ["Erschlossen"],
                ImageUrls = ["https://picsum.photos/seed/zwang2/800/600"]
            },

            // Weitere Haeuser
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Bungalow in Braunau",
                Address = "Gartenstrasse 18",
                City = "Braunau am Inn",
                PostalCode = "5280",
                Price = 275000,
                LivingAreaSquareMeters = 95,
                PlotAreaSquareMeters = 600,
                Rooms = 3,
                YearBuilt = 2005,
                Type = PropertyType.House,
                SellerType = SellerType.Privat,
                SellerName = "Ehepaar Schmidt",
                Description = "Barrierefreier Bungalow, ideal fuer Senioren. Pflegeleichter Garten.",
                Features = ["Carport", "Garten", "Barrierefrei", "Fussbodenheizung"],
                ImageUrls = ["https://picsum.photos/seed/bungalow1/800/600"]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Doppelhaushälfte Vöcklabruck",
                Address = "Schulweg 7",
                City = "Voecklabruck",
                PostalCode = "4840",
                Price = 298000,
                LivingAreaSquareMeters = 125,
                PlotAreaSquareMeters = 280,
                Rooms = 4,
                YearBuilt = 2019,
                Type = PropertyType.House,
                SellerType = SellerType.Makler,
                SellerName = "Hausfreund Immobilien",
                Description = "Neuwertige Doppelhaushaelfte in familienfreundlicher Lage. Schulen und Kindergarten in Gehweite.",
                Features = ["Garage", "Garten", "Terrasse", "Fussbodenheizung", "Waermepumpe"],
                ImageUrls = ["https://picsum.photos/seed/doppel1/800/600"]
            }
        };

        // UserId und CreatedAt fuer alle Properties setzen
        foreach (var property in properties)
        {
            property.UserId = GetNextSellerId();
            property.CreatedAt = now;
        }

        // TypeSpecificData fuer alle Properties setzen
        SetTypeSpecificData(properties);

        dbContext.Set<Property>().AddRange(properties);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Setzt TypeSpecificData fuer alle Properties basierend auf ihrem Typ
    /// </summary>
    private static void SetTypeSpecificData(List<Property> properties)
    {
        foreach (var property in properties)
        {
            switch (property.Type)
            {
                case PropertyType.House:
                    // Realistic house data
                    var houseData = new HousePropertyData(
                        LivingAreaInSquareMeters: property.LivingAreaSquareMeters ?? 100,
                        TotalRooms: property.Rooms ?? 4,
                        Bedrooms: Math.Max(1, (property.Rooms ?? 4) - 2), // Rooms - 2 for living/dining
                        Bathrooms: property.Rooms >= 5 ? 2 : 1,
                        YearBuilt: property.YearBuilt,
                        Floors: property.LivingAreaSquareMeters > 150 ? 2 : 1,
                        Condition: property.YearBuilt switch
                        {
                            >= 2015 => PropertyCondition.LikeNew,
                            >= 2000 => PropertyCondition.Good,
                            >= 1990 => PropertyCondition.Average,
                            _ => PropertyCondition.NeedsRenovation
                        },
                        HasGarage: property.Features.Any(a => a.Contains("Garage") || a.Contains("garage")),
                        HasGarden: property.Features.Any(a => a.Contains("Garten") || a.Contains("garden")),
                        ApartmentFloor: null,
                        HasElevator: null,
                        HasBasement: property.Features.Any(a => a.Contains("Keller") || a.Contains("basement"))
                    );
                    property.SetTypedData(houseData);
                    break;

                case PropertyType.Land:
                    // Realistic land data
                    var landData = new LandPropertyData(
                        PlotSizeInSquareMeters: property.PlotAreaSquareMeters ?? 800,
                        Zoning: property.City switch
                        {
                            "Linz" or "Wels" => ZoningType.Residential,
                            "Leonding" => ZoningType.Mixed,
                            _ => ZoningType.Residential
                        },
                        HasBuildingRights: property.Features.Any(a => a.Contains("Erschlossen") || a.Contains("erschlossen")),
                        SoilQuality: property.Price > 200000 ? SoilQuality.High : SoilQuality.Medium,
                        IsBuildable: property.Features.Any(a => a.Contains("Erschlossen") || a.Contains("erschlossen")),
                        HasUtilities: property.Features.Any(a => a.Contains("Wasser") || a.Contains("Strom") || a.Contains("water") || a.Contains("electricity"))
                    );
                    property.SetTypedData(landData);
                    break;

                case PropertyType.Foreclosure:
                    // Realistic foreclosure data
                    var foreclosureData = new ForeclosurePropertyData(
                        CourtName: property.SellerName,
                        AuctionDate: DateTime.Now.AddDays(30 + Random.Shared.Next(0, 60)),
                        MinimumBid: property.Price * 0.7m, // 70% of market value
                        Encumbrances:
                        [
                            new Encumbrance("Hypothek Bank Austria", property.Price * 0.5m, "Bank Austria"),
                            new Encumbrance("Grundsteuer", 2500, "Finanzamt")
                        ],
                        Status: LegalStatus.Scheduled,
                        FileNumber: $"AZ-{Random.Shared.Next(1000, 9999)}/{DateTime.Now.Year}"
                    );
                    property.SetTypedData(foreclosureData);
                    break;
            }
        }
    }
}
