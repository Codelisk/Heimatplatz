using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Locations.Data.Entities;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Enums;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific.Enums;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.Properties.Data.Seeding;

/// <summary>
/// Seeder für Beispiel-Immobilien in Oberösterreich.
/// Die Bilder sind kuratierte, lizenzfreie Fotos aus wwwroot/seed - stabile URLs,
/// damit Store-Screenshots (Cake Android-/IosScreenshots) immer gleich aussehen.
/// </summary>
public class PropertySeeder(AppDbContext dbContext, IConfiguration configuration, ILogger<PropertySeeder> logger) : ISeeder
{
    public int Order => 10;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Set<Property>().AnyAsync(cancellationToken))
            return;

        // Verkäufer abrufen (User mit gesetztem SellerType)
        var sellers = await dbContext.Set<User>()
            .Where(u => u.SellerType != null)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (sellers.Count == 0)
        {
            // Keine Seller vorhanden - Seeding überspringen
            return;
        }

        // Municipalities laden für FK-Zuordnung (Umlaut-normalisiert, Duplikat-sicher:
        // gleichnamige Gemeinden in verschiedenen Bezirken dürfen das Seeding nicht crashen)
        var municipalityList = await dbContext.Set<Municipality>().ToListAsync(cancellationToken);

        if (municipalityList.Count == 0)
        {
            // Keine Municipalities vorhanden - Seeding überspringen
            return;
        }

        var municipalities = municipalityList
            .GroupBy(m => MunicipalityNameResolver.NormalizeCityKey(m.Name))
            .ToDictionary(g => g.Key, g => g.First().Id);

        // Helper: Municipality ID by Name. Kein stiller Fallback - eine falsche Zuordnung
        // macht den Ort-Filter kaputt (Objekt erscheint bei fremden Gemeinden, fehlt bei der eigenen).
        Guid? GetMunicipalityId(string cityName)
        {
            var id = MunicipalityNameResolver.Resolve(municipalities, cityName);
            if (id == null)
                logger.LogError("[PropertySeeder] Gemeinde '{City}' nicht auflösbar - Objekt wird übersprungen", cityName);
            return id;
        }

        // Properties gleichmäßig auf Seller verteilen
        var sellerIndex = 0;
        Guid GetNextSellerId()
        {
            var sellerId = sellers[sellerIndex];
            sellerIndex = (sellerIndex + 1) % sellers.Count;
            return sellerId;
        }

        List<string> Img(params string[] files) => BuildSeedImageUrls(configuration, files);

        var now = DateTimeOffset.UtcNow;
        var seedCandidates = new List<Property?>
        {
            // Häuser
            CreateProperty("Einfamilienhaus in Linz-Urfahr", "Hauptstraße 15", "Linz", 349000,
                145, 520, 5, 2018, PropertyType.House, SellerType.Broker, "Mustermann Immobilien",
                "Wunderschönes Einfamilienhaus mit großem Garten in ruhiger Lage. Hochwertige Ausstattung, Fußbodenheizung, Photovoltaikanlage.",
                ["Garage", "Garten", "Terrasse", "Keller", "Fußbodenheizung", "Photovoltaik"],
                Img("haus-aelter.jpg", "interieur-wohnkueche.jpg", "interieur-altbau.jpg"), GetMunicipalityId),

            CreateProperty("Modernes Reihenhaus in Wels", "Ringstraße 42", "Wels", 289000,
                120, 180, 4, 2020, PropertyType.House, SellerType.Private, "Familie Huber",
                "Neuwertiges Reihenhaus in zentraler Lage. Perfekt für junge Familien. Kurze Wege zu Schulen und Geschäften.",
                ["Carport", "Terrasse", "Keller", "Fußbodenheizung"],
                Img("haus-daemmerung.jpg", "interieur-altbau.jpg"), GetMunicipalityId),

            CreateProperty("Villa am Traunsee", "Seeuferweg 8", "Gmunden", 890000,
                220, 1200, 7, 2015, PropertyType.House, SellerType.Broker, "Luxus Immobilien GmbH",
                "Exklusive Villa mit direktem Seezugang. Panoramablick auf den Traunsee. Hochwertigste Ausstattung.",
                ["Doppelgarage", "Sauna", "Seezugang", "Bootshaus", "Kamin"],
                Img("see-berge.jpg", "berg-see.jpg", "interieur-schlafzimmer.jpg"), GetMunicipalityId),

            CreateProperty("Landhaus in Bad Ischl", "Kaiserweg 23", "Bad Ischl", 425000,
                165, 850, 5, 1998, PropertyType.House, SellerType.Private, "Herr Maier",
                "Charmantes Landhaus im Salzkammergut. Renoviert mit Liebe zum Detail. Idealer Rückzugsort.",
                ["Garage", "Garten", "Kachelofen", "Keller", "Dachboden"],
                Img("haus-chalet.jpg", "bauernhaus.jpg", "interieur-rustikal.jpg"), GetMunicipalityId),

            CreateProperty("Familienhaus in Steyr", "Bahnhofstraße 67", "Steyr", 315000,
                135, 450, 5, 2010, PropertyType.House, SellerType.Broker, "Immobilien Steyr",
                "Gepflegtes Einfamilienhaus in guter Lage. Nahe Stadtzentrum und Naturgebiet.",
                ["Garage", "Garten", "Terrasse", "Keller"],
                Img("haus-teich.jpg", "interieur-wohnkueche.jpg"), GetMunicipalityId),

            // Grundstücke
            CreateProperty("Baugrundstück in Wels", "Neubaugebiet Süd", "Wels", 189000,
                null, 850, null, null, PropertyType.Land, SellerType.Private, "Familie Müller",
                "Voll erschlossenes Baugrundstück in ruhiger Wohnlage. Alle Anschlüsse vorhanden.",
                ["Erschlossen", "Strom", "Wasser", "Kanal", "Gas"],
                Img("grund-feld.jpg"), GetMunicipalityId),

            CreateProperty("Sonniges Baugrundstück Linz-Land", "Am Sonnenhang 12", "Leonding", 245000,
                null, 720, null, null, PropertyType.Land, SellerType.Broker, "Grund & Boden OÖ",
                "Südhanglage mit herrlichem Ausblick. Bebauungsplan liegt vor.",
                ["Erschlossen", "Südlage", "Aussicht"],
                Img("huegel-strasse.jpg", "berge.jpg"), GetMunicipalityId),

            CreateProperty("Großes Baugrundstück Mühlviertel", "Dorfstraße", "Freistadt", 95000,
                null, 1200, null, null, PropertyType.Land, SellerType.Private, "Gemeinde Freistadt",
                "Günstiges Baugrundstück im schönen Mühlviertel. Ruhige Lage, gute Infrastruktur.",
                ["Teilerschlossen", "Strom", "Wasser"],
                Img("grund-wald.jpg", "huegel-nebel.jpg"), GetMunicipalityId),

            // Zwangsversteigerungen
            CreateProperty("Zwangsversteigerung: Haus in Traun", "Industriestraße 45", "Traun", 185000,
                110, 380, 4, 1985, PropertyType.Foreclosure, SellerType.Broker, "Bezirksgericht Linz",
                "Älteres Haus mit Renovierungsbedarf. Versteigerungstermin: nächsten Monat. Besichtigung möglich.",
                ["Garage", "Keller"],
                Img("bauernhof-alt.jpg"), GetMunicipalityId),

            CreateProperty("Zwangsversteigerung: Grundstück Enns", "Feldweg 3", "Enns", 68000,
                null, 650, null, null, PropertyType.Foreclosure, SellerType.Broker, "Bezirksgericht Steyr",
                "Baugrundstück aus Zwangsversteigerung. Gute Lage, erschlossen.",
                ["Erschlossen"],
                Img("grund-sonnenuntergang.jpg"), GetMunicipalityId),

            // Weitere Häuser
            CreateProperty("Bungalow in Braunau", "Gartenstraße 18", "Braunau am Inn", 275000,
                95, 600, 3, 2005, PropertyType.House, SellerType.Private, "Ehepaar Schmidt",
                "Barrierefreier Bungalow, ideal für Senioren. Pflegeleichter Garten.",
                ["Carport", "Garten", "Barrierefrei", "Fußbodenheizung"],
                Img("haus-wiese.jpg", "interieur-rustikal.jpg"), GetMunicipalityId),

            CreateProperty("Doppelhaushälfte Vöcklabruck", "Schulweg 7", "Vöcklabruck", 298000,
                125, 280, 4, 2019, PropertyType.House, SellerType.Broker, "Hausfreund Immobilien",
                "Neuwertige Doppelhaushälfte in familienfreundlicher Lage. Schulen und Kindergarten in Gehweite.",
                ["Garage", "Garten", "Terrasse", "Fußbodenheizung", "Wärmepumpe"],
                Img("haus-landhaus.jpg", "interieur-altbau.jpg"), GetMunicipalityId),

            CreateProperty("Einfamilienhaus am Traunfall", "Traunfallstraße 12", "Roitham am Traunfall", 365000,
                140, 610, 5, 2012, PropertyType.House, SellerType.Private, "Familie Berger",
                "Gepflegtes Einfamilienhaus in ruhiger Siedlungslage nahe dem Traunfall. Großer Garten mit altem Baumbestand.",
                ["Garage", "Garten", "Terrasse", "Keller", "Kachelofen"],
                Img("almhuette.jpg", "wiese-blumen.jpg", "interieur-schlafzimmer.jpg"), GetMunicipalityId)
        };

        var properties = seedCandidates.OfType<Property>().ToList();

        // UserId und CreatedAt für alle Properties setzen
        // Unterschiedliche CreatedAt-Zeiten für Age-Filter Tests
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

        // TypeSpecificData für alle Properties setzen
        SetTypeSpecificData(properties);

        // Kontaktdaten für alle Properties generieren
        SetContactData(properties, configuration);

        // SellerSource-FK für gewerbliche Anbieter (Makler/Verwaltung) auflösen/anlegen,
        // damit der Anbieter-Ausschlussfilter auch die Demo-Inserate erfasst
        await ResolveSellerSourcesAsync(properties, cancellationToken);

        dbContext.Set<Property>().AddRange(properties);

        // Explizit alle Contacts zum DbContext hinzufügen (EF Core Cascade-Fix)
        var allContacts = properties.SelectMany(p => p.Contacts).ToList();
        dbContext.Set<PropertyContactInfo>().AddRange(allContacts);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Löst für alle gewerblichen Demo-Inserate (Broker/Verwaltung) die SellerSource
    /// per Name auf bzw. legt fehlende Einträge an und setzt die FK.
    /// </summary>
    private async Task ResolveSellerSourcesAsync(List<Property> properties, CancellationToken cancellationToken)
    {
        var commercial = properties
            .Where(p => p.SellerType is SellerType.Broker or SellerType.PropertyManager)
            .ToList();

        if (commercial.Count == 0)
            return;

        var names = commercial.Select(p => p.SellerName).Distinct().ToList();

        var sources = await dbContext.Set<SellerSource>()
            .Where(ss => names.Contains(ss.Name))
            .ToDictionaryAsync(ss => ss.Name, ss => ss.Id, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var name in names.Where(n => !sources.ContainsKey(n)))
        {
            var source = new SellerSource
            {
                Id = Guid.NewGuid(),
                Name = name,
                IsDefault = true,
                CreatedAt = now
            };
            dbContext.Set<SellerSource>().Add(source);
            sources[name] = source.Id;
        }

        foreach (var property in commercial)
        {
            property.SellerSourceId = sources[property.SellerName];
        }
    }

    /// <summary>
    /// Kuratierte Seed-Fotos aus wwwroot/seed - absolute URLs, weil Clients
    /// (Web-Proxy, MAUI-ImageCache) vollqualifizierte Bild-URLs erwarten.
    /// Api:PublicBaseUrl kommt im Docker-Deploy aus der Compose-Env.
    /// </summary>
    internal static List<string> BuildSeedImageUrls(Microsoft.Extensions.Configuration.IConfiguration configuration, params string[] files)
    {
        var baseUrl = (configuration["Api:PublicBaseUrl"] ?? "http://localhost:5292").TrimEnd('/');
        return files.Select(f => $"{baseUrl}/seed/{f}").ToList();
    }

    /// <summary>
    /// Helper: Create a Property with MunicipalityId lookup.
    /// Liefert null wenn die Gemeinde nicht auflösbar ist (Objekt wird übersprungen).
    /// Internal: wird auch vom PropertyMunicipalityFixSeeder für nachgelegte Objekte genutzt.
    /// </summary>
    internal static Property? CreateProperty(
        string title, string address, string cityName, decimal price,
        int? livingArea, int? plotArea, int? rooms, int? yearBuilt,
        PropertyType type, SellerType sellerType, string sellerName,
        string description, List<string> features, List<string> imageUrls,
        Func<string, Guid?> getMunicipalityId)
    {
        var municipalityId = getMunicipalityId(cityName);
        if (municipalityId == null)
            return null;

        return new Property
        {
            Id = Guid.NewGuid(),
            Title = title,
            Address = address,
            MunicipalityId = municipalityId.Value,
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
            ImageUrls = imageUrls
        };
    }

    /// <summary>
    /// Setzt TypeSpecificData für alle Properties basierend auf ihrem Typ
    /// </summary>
    internal static void SetTypeSpecificData(List<Property> properties)
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
                        CadastralMunicipality: "Oberösterreich",
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
    /// Generiert Kontaktdaten für alle Properties
    /// </summary>
    internal static void SetContactData(List<Property> properties, IConfiguration configuration)
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

            // Jedes Seed-Inserat verlinkt sein "Originalinserat" - wie vom WYSIWYG-Editor
            // erzeugt haengt die URL am Erst-Kontakt (ohne SourceName)
            var (originalSourceName, originalListingUrl) = BuildOriginalListingLink(configuration, property.Title);

            var mainContact = new PropertyContactInfo
            {
                Id = Guid.NewGuid(),
                PropertyId = property.Id,
                Type = contactType,
                Source = contactSource,
                Name = property.SellerName,
                Email = $"{emailName}@{emailDomains[Random.Shared.Next(emailDomains.Length)]}",
                Phone = $"{phonePrefix[Random.Shared.Next(phonePrefix.Length)]} {Random.Shared.Next(1000000, 9999999)}",
                OriginalListingUrl = originalListingUrl,
                SourceName = null,
                DisplayOrder = 0,
                CreatedAt = property.CreatedAt
            };

            property.Contacts.Add(mainContact);

            // Bei Maklern: zusätzlich Eigentümer als zweiten Kontakt (50% Chance)
            if (property.SellerType == SellerType.Broker && Random.Shared.Next(2) == 0)
            {
                var ownerNames = new[] { "Herr Müller", "Frau Schmidt", "Familie Weber", "Herr Huber", "Frau Maier" };
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

            // Bei einigen Properties: zusaetzlicher Import-Kontakt mit derselben Quelle (30% Chance)
            if (Random.Shared.Next(10) < 3)
            {
                var importContact = new PropertyContactInfo
                {
                    Id = Guid.NewGuid(),
                    PropertyId = property.Id,
                    Type = ContactType.Agent,
                    Source = ContactSource.Import,
                    Name = property.SellerName,
                    OriginalListingUrl = originalListingUrl,
                    SourceName = originalSourceName,
                    SourceId = originalListingUrl[(originalListingUrl.LastIndexOf('=') + 1)..],
                    DisplayOrder = property.Contacts.Count,
                    CreatedAt = property.CreatedAt
                };

                property.Contacts.Add(importContact);
            }
        }
    }

    /// <summary>
    /// Anonymer Original-Inserat-Beispiel-Link pro Seed-Objekt: zeigt intern auf die
    /// eigene Webseite (/beispiel-originalinserat), keine echten Fremdportale.
    /// Deterministisch (stabiler Titel-Hash statt string.GetHashCode, das pro Prozess
    /// randomisiert ist), damit Backfill und Erst-Seeding dieselbe URL erzeugen.
    /// </summary>
    internal static (string SourceName, string Url) BuildOriginalListingLink(IConfiguration configuration, string title)
    {
        var webBaseUrl = (configuration["Email:FrontendBaseUrl"] ?? "https://heimatplatz.at").TrimEnd('/');

        var hash = 0;
        foreach (var c in title)
            hash = unchecked(hash * 31 + c);

        var sourceId = 100000 + Math.Abs(hash) % 900000;
        return ("Beispiel-Portal", $"{webBaseUrl}/beispiel-originalinserat?nr={sourceId}");
    }
}
