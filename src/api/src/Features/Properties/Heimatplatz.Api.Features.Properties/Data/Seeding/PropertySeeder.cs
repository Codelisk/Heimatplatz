using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Auth.Contracts.Enums;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Locations.Data.Entities;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Enums;
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

        // Municipalities laden fuer FK-Zuordnung
        var municipalities = await dbContext.Set<Municipality>()
            .ToDictionaryAsync(m => m.Name.ToLowerInvariant(), m => m.Id, cancellationToken);

        if (municipalities.Count == 0)
        {
            // Keine Municipalities vorhanden - Seeding ueberspringen
            return;
        }

        // Helper: Municipality ID by Name (case-insensitive, partial match)
        Guid GetMunicipalityId(string cityName)
        {
            var key = cityName.ToLowerInvariant();
            if (municipalities.TryGetValue(key, out var id))
                return id;

            // Partial match (e.g. "Braunau am Inn" -> "Braunau")
            var partialMatch = municipalities.Keys.FirstOrDefault(k => k.Contains(key) || key.Contains(k));
            if (partialMatch != null)
                return municipalities[partialMatch];

            // Fallback: erste Municipality
            return municipalities.Values.First();
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
            CreateProperty("Einfamilienhaus in Linz-Urfahr", "Hauptstrasse 15", "Linz", 349000,
                145, 520, 5, 2018, PropertyType.House, SellerType.Broker, "Mustermann Immobilien",
                "Wunderschoenes Einfamilienhaus mit grossem Garten in ruhiger Lage. Hochwertige Ausstattung, Fussbodenheizung, Photovoltaikanlage.",
                ["Garage", "Garten", "Terrasse", "Keller", "Fussbodenheizung", "Photovoltaik"],
                "https://picsum.photos/seed/haus1/800/600", GetMunicipalityId),

            CreateProperty("Modernes Reihenhaus in Wels", "Ringstrasse 42", "Wels", 289000,
                120, 180, 4, 2020, PropertyType.House, SellerType.Private, "Familie Huber",
                "Neuwertiges Reihenhaus in zentraler Lage. Perfekt fuer junge Familien. Kurze Wege zu Schulen und Geschaeften.",
                ["Carport", "Terrasse", "Keller", "Fussbodenheizung"],
                "https://picsum.photos/seed/haus2/800/600", GetMunicipalityId),

            CreateProperty("Villa am Traunsee", "Seeuferweg 8", "Gmunden", 890000,
                220, 1200, 7, 2015, PropertyType.House, SellerType.Broker, "Luxus Immobilien GmbH",
                "Exklusive Villa mit direktem Seezugang. Panoramablick auf den Traunsee. Hochwertigste Ausstattung.",
                ["Doppelgarage", "Pool", "Sauna", "Seezugang", "Smarthome", "Klimaanlage"],
                "https://picsum.photos/seed/villa1/800/600", GetMunicipalityId),

            CreateProperty("Landhaus in Bad Ischl", "Kaiserweg 23", "Bad Ischl", 425000,
                165, 850, 5, 1998, PropertyType.House, SellerType.Private, "Herr Maier",
                "Charmantes Landhaus im Salzkammergut. Renoviert mit Liebe zum Detail. Idealer Rueckzugsort.",
                ["Garage", "Garten", "Kachelofen", "Keller", "Dachboden"],
                "https://picsum.photos/seed/landhaus1/800/600", GetMunicipalityId),

            CreateProperty("Familienhaus in Steyr", "Bahnhofstrasse 67", "Steyr", 315000,
                135, 450, 5, 2010, PropertyType.House, SellerType.Broker, "Immobilien Steyr",
                "Gepflegtes Einfamilienhaus in guter Lage. Nahe Stadtzentrum und Naturgebiet.",
                ["Garage", "Garten", "Terrasse", "Keller"],
                "https://picsum.photos/seed/haus3/800/600", GetMunicipalityId),

            // Grundstuecke
            CreateProperty("Baugrundstück in Wels", "Neubaugebiet Sued", "Wels", 189000,
                null, 850, null, null, PropertyType.Land, SellerType.Private, "Familie Mueller",
                "Voll erschlossenes Baugrundstuck in ruhiger Wohnlage. Alle Anschluesse vorhanden.",
                ["Erschlossen", "Strom", "Wasser", "Kanal", "Gas"],
                "https://picsum.photos/seed/grund1/800/600", GetMunicipalityId),

            CreateProperty("Sonniges Baugrundstück Linz-Land", "Am Sonnenhang 12", "Leonding", 245000,
                null, 720, null, null, PropertyType.Land, SellerType.Broker, "Grund & Boden OOe",
                "Suedhanglage mit herrlichem Ausblick. Bebauungsplan liegt vor.",
                ["Erschlossen", "Suedlage", "Aussicht"],
                "https://picsum.photos/seed/grund2/800/600", GetMunicipalityId),

            CreateProperty("Grosses Baugrundstück Muehlviertel", "Dorfstrasse", "Freistadt", 95000,
                null, 1200, null, null, PropertyType.Land, SellerType.Private, "Gemeinde Freistadt",
                "Guenstiges Baugrundstuck im schoenen Muehlviertel. Ruhige Lage, gute Infrastruktur.",
                ["Teilerschlossen", "Strom", "Wasser"],
                "https://picsum.photos/seed/grund3/800/600", GetMunicipalityId),

            // Zwangsversteigerungen
            CreateProperty("Zwangsversteigerung: Haus in Traun", "Industriestrasse 45", "Traun", 185000,
                110, 380, 4, 1985, PropertyType.Foreclosure, SellerType.Broker, "Bezirksgericht Linz",
                "Aelteres Haus mit Renovierungsbedarf. Versteigerungstermin: naechsten Monat. Besichtigung moeglich.",
                ["Garage", "Keller"],
                "https://picsum.photos/seed/zwang1/800/600", GetMunicipalityId),

            CreateProperty("Zwangsversteigerung: Grundstück Enns", "Feldweg 3", "Enns", 68000,
                null, 650, null, null, PropertyType.Foreclosure, SellerType.Broker, "Bezirksgericht Steyr",
                "Baugrundstuck aus Zwangsversteigerung. Gute Lage, erschlossen.",
                ["Erschlossen"],
                "https://picsum.photos/seed/zwang2/800/600", GetMunicipalityId),

            // Weitere Haeuser
            CreateProperty("Bungalow in Braunau", "Gartenstrasse 18", "Braunau am Inn", 275000,
                95, 600, 3, 2005, PropertyType.House, SellerType.Private, "Ehepaar Schmidt",
                "Barrierefreier Bungalow, ideal fuer Senioren. Pflegeleichter Garten.",
                ["Carport", "Garten", "Barrierefrei", "Fussbodenheizung"],
                "https://picsum.photos/seed/bungalow1/800/600", GetMunicipalityId),

            CreateProperty("Doppelhaushälfte Vöcklabruck", "Schulweg 7", "Voecklabruck", 298000,
                125, 280, 4, 2019, PropertyType.House, SellerType.Broker, "Hausfreund Immobilien",
                "Neuwertige Doppelhaushaelfte in familienfreundlicher Lage. Schulen und Kindergarten in Gehweite.",
                ["Garage", "Garten", "Terrasse", "Fussbodenheizung", "Waermepumpe"],
                "https://picsum.photos/seed/doppel1/800/600", GetMunicipalityId)
        };

        // UserId und CreatedAt fuer alle Properties setzen
        // Unterschiedliche CreatedAt-Zeiten fuer Age-Filter Tests
        var createdDates = new[]
        {
            now.AddHours(-2),        // Heute
            now.AddHours(-20),       // Gestern
            now.AddDays(-5),         // 5 Tage
            now.AddDays(-14),        // 2 Wochen
            now.AddMonths(-2),       // 2 Monate
            now.AddMonths(-6),       // 6 Monate
            now.AddDays(-3),         // 3 Tage
            now.AddDays(-10),        // 10 Tage
            now.AddMonths(-1),       // 1 Monat
            now.AddDays(-1),         // 1 Tag
            now.AddMonths(-4),       // 4 Monate
            now.AddYears(-1).AddDays(10), // ~1 Jahr
            now.AddDays(-7),         // 1 Woche
        };

        for (var i = 0; i < properties.Count; i++)
        {
            properties[i].UserId = GetNextSellerId();
            properties[i].CreatedAt = createdDates[i % createdDates.Length];
        }

        // TypeSpecificData fuer alle Properties setzen
        SetTypeSpecificData(properties);

        // Kontaktdaten fuer alle Properties generieren
        SetContactData(properties);

        dbContext.Set<Property>().AddRange(properties);

        // Explizit alle Contacts zum DbContext hinzufuegen (EF Core Cascade-Fix)
        var allContacts = properties.SelectMany(p => p.Contacts).ToList();
        dbContext.Set<PropertyContactInfo>().AddRange(allContacts);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Helper: Create a Property with MunicipalityId lookup
    /// </summary>
    private static Property CreateProperty(
        string title, string address, string cityName, decimal price,
        int? livingArea, int? plotArea, int? rooms, int? yearBuilt,
        PropertyType type, SellerType sellerType, string sellerName,
        string description, List<string> features, string imageUrl,
        Func<string, Guid> getMunicipalityId)
    {
        return new Property
        {
            Id = Guid.NewGuid(),
            Title = title,
            Address = address,
            MunicipalityId = getMunicipalityId(cityName),
            Price = price,
            LivingAreaSquareMeters = livingArea,
            PlotAreaSquareMeters = plotArea,
            Rooms = rooms,
            YearBuilt = yearBuilt,
            Type = type,
            SellerType = sellerType,
            SellerName = sellerName,
            Description = description,
            Features = features,
            ImageUrls = [imageUrl]
        };
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
                        Zoning: property.Price > 200000 ? ZoningType.Mixed : ZoningType.Residential,
                        HasBuildingRights: property.Features.Any(a => a.Contains("Erschlossen") || a.Contains("erschlossen")),
                        SoilQuality: property.Price > 200000 ? SoilQuality.High : SoilQuality.Medium,
                        IsBuildable: property.Features.Any(a => a.Contains("Erschlossen") || a.Contains("erschlossen")),
                        HasUtilities: property.Features.Any(a => a.Contains("Wasser") || a.Contains("Strom") || a.Contains("water") || a.Contains("electricity"))
                    );
                    property.SetTypedData(landData);
                    break;

                case PropertyType.Foreclosure:
                    // Realistic foreclosure data with all fields populated
                    var estimatedValue = property.Price * 1.2m; // Estimated value higher than minimum bid
                    var auctionDate = DateTime.Now.AddDays(30 + Random.Shared.Next(0, 60));
                    var foreclosureData = new ForeclosurePropertyData(
                        CourtName: property.SellerName,
                        AuctionDate: auctionDate,
                        MinimumBid: property.Price * 0.7m, // 70% of market value
                        EstimatedValue: estimatedValue,
                        Encumbrances:
                        [
                            new Encumbrance("Hypothek Bank Austria", property.Price * 0.5m, "Bank Austria"),
                            new Encumbrance("Grundsteuer", 2500, "Finanzamt")
                        ],
                        Status: LegalStatus.Scheduled,
                        FileNumber: $"{Random.Shared.Next(100, 999)} E {Random.Shared.Next(100, 999)}/{DateTime.Now.Year % 100}",
                        RegistrationNumber: $"EZ {Random.Shared.Next(1000, 9999)}/{DateTime.Now.Year}",
                        CadastralMunicipality: "Oberoesterreich",
                        PlotNumber: $"{Random.Shared.Next(100, 999)}/{Random.Shared.Next(1, 20)}",
                        TotalArea: property.PlotAreaSquareMeters ?? 500,
                        BuildingArea: property.LivingAreaSquareMeters ?? 0,
                        ZoningDesignation: "Bauland-Wohngebiet",
                        BuildingCondition: property.YearBuilt switch
                        {
                            >= 2010 => "Gut erhalten",
                            >= 2000 => "Gepflegt, teilweise modernisiert",
                            >= 1990 => "Sanierungsbedarf",
                            _ => "Renovierungsbedürftig"
                        },
                        NumberOfRooms: property.Rooms,
                        YearBuilt: property.YearBuilt,
                        ViewingDate: auctionDate.AddDays(-7),
                        BiddingDeadline: auctionDate.AddDays(-1),
                        OwnershipShare: "1/1",
                        Notes: "Besichtigung nach Voranmeldung beim Gericht möglich. Weitere Informationen im Edikt.",
                        EdictUrl: $"https://edikte.justiz.gv.at/sample-{property.Id}.odt"
                    );
                    property.SetTypedData(foreclosureData);
                    break;
            }
        }
    }

    /// <summary>
    /// Generiert Kontaktdaten fuer alle Properties
    /// </summary>
    private static void SetContactData(List<Property> properties)
    {
        var emailDomains = new[] { "gmail.com", "gmx.at", "outlook.com", "immobilien.at" };
        var phonePrefix = new[] { "+43 650", "+43 664", "+43 676", "+43 699" };

        foreach (var property in properties)
        {
            property.InquiryType = InquiryType.ContactData;

            // Main contact based on SellerType
            var contactType = property.SellerType switch
            {
                SellerType.Broker => ContactType.Agent,
                _ => ContactType.Seller
            };

            var contactSource = ContactSource.Manual;

            var nameParts = property.SellerName.Split(' ');
            var emailName = nameParts.Length > 1
                ? $"{nameParts[0].ToLower()}.{nameParts[^1].ToLower()}"
                : property.SellerName.ToLower().Replace(" ", ".");

            var mainContact = new PropertyContactInfo
            {
                Id = Guid.NewGuid(),
                PropertyId = property.Id,
                Type = contactType,
                Source = contactSource,
                Name = property.SellerName,
                Email = $"{emailName}@{emailDomains[Random.Shared.Next(emailDomains.Length)]}",
                Phone = $"{phonePrefix[Random.Shared.Next(phonePrefix.Length)]} {Random.Shared.Next(1000000, 9999999)}",
                OriginalListingUrl = null,
                SourceName = null,
                DisplayOrder = 0,
                CreatedAt = property.CreatedAt
            };

            property.Contacts.Add(mainContact);

            // Bei Maklern: zusaetzlich Eigentuemer als zweiten Kontakt (50% Chance)
            if (property.SellerType == SellerType.Broker && Random.Shared.Next(2) == 0)
            {
                var ownerNames = new[] { "Herr Mueller", "Frau Schmidt", "Familie Weber", "Herr Huber", "Frau Maier" };
                var ownerName = ownerNames[Random.Shared.Next(ownerNames.Length)];

                var ownerContact = new PropertyContactInfo
                {
                    Id = Guid.NewGuid(),
                    PropertyId = property.Id,
                    Type = ContactType.Seller,
                    Source = ContactSource.Manual,
                    Name = ownerName,
                    Phone = $"{phonePrefix[Random.Shared.Next(phonePrefix.Length)]} {Random.Shared.Next(1000000, 9999999)}",
                    DisplayOrder = 1,
                    CreatedAt = property.CreatedAt
                };

                property.Contacts.Add(ownerContact);
            }

            // Bei einigen Properties: Import-Quelle mit Original-Link (30% Chance)
            if (Random.Shared.Next(10) < 3)
            {
                var importSources = new[] {
                    ("ImmoScout24", "https://www.immobilienscout24.at/expose/"),
                    ("willhaben", "https://www.willhaben.at/iad/immobilien/d/"),
                    ("ImmobilienNET", "https://www.immobilien.net/expose/")
                };

                var (sourceName, baseUrl) = importSources[Random.Shared.Next(importSources.Length)];
                var sourceId = Random.Shared.Next(100000, 999999).ToString();

                var importContact = new PropertyContactInfo
                {
                    Id = Guid.NewGuid(),
                    PropertyId = property.Id,
                    Type = ContactType.Agent,
                    Source = ContactSource.Import,
                    Name = property.SellerName,
                    OriginalListingUrl = $"{baseUrl}{sourceId}",
                    SourceName = sourceName,
                    SourceId = sourceId,
                    DisplayOrder = property.Contacts.Count,
                    CreatedAt = property.CreatedAt
                };

                property.Contacts.Add(importContact);
            }
        }
    }
}
