using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts;
using Heimatplatz.Api.Features.ForeclosureAuctions.Data.Entities;
using Heimatplatz.Api.Features.Locations.Data.Entities;
using Heimatplatz.Api.Features.Notifications.Contracts.Events;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Enums;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific.Enums;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Heimatplatz.Api.Features.Properties.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Services;

public class ForeclosurePropertySyncService(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IHttpClientFactory httpClientFactory,
    ILoggerFactory loggerFactory,
    IPropertyGeocoder propertyGeocoder,
    IMediator mediator,
    ILogger<ForeclosurePropertySyncService> logger
) : IForeclosurePropertySyncService
{
    // Deckel pro Sync-Lauf: das Geocoding ist auf 1 Request/Sekunde gedrosselt -
    // ohne Deckel wuerde der Erstlauf mit vielen Auctions minutenlang haengen.
    // Der Rest kommt beim naechsten Sync bzw. ueber den Admin-Backfill dran.
    private const int MaxGeocodesPerSync = 25;

    public async Task<PropertySyncResult> SyncToPropertiesAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starte Property-Sync aus Zwangsversteigerungen");

        var created = 0;
        var updated = 0;
        var removed = 0;
        var skipped = 0;
        var errors = 0;
        var errorMessages = new List<string>();

        // 1. System-User sicherstellen
        var systemUserId = ForeclosureAuctionConstants.SystemUserId;
        if (!await dbContext.Set<User>().AnyAsync(u => u.Id == systemUserId, ct))
        {
            logger.LogInformation("System-User existiert nicht, wird erstellt");
            var systemUser = new User
            {
                Id = systemUserId,
                FirstName = "System",
                LastName = "Heimatplatz",
                Email = "system@heimatplatz.at",
                PasswordHash = passwordHasher.Hash(Guid.NewGuid().ToString()),
                CreatedAt = DateTimeOffset.UtcNow
            };
            dbContext.Set<User>().Add(systemUser);
            await dbContext.SaveChangesAsync(ct);
        }

        // 2. Municipalities laden fuer PLZ/Ort-Aufloesung
        var municipalities = await dbContext.Set<Municipality>().ToListAsync(ct);
        if (municipalities.Count == 0)
        {
            logger.LogWarning("Keine Municipalities - versuche auto-seed von OpenPLZ API");
            try
            {
                var seeder = new Heimatplatz.Api.Features.Locations.Data.Seeding.LocationSeeder(
                    dbContext, httpClientFactory, loggerFactory.CreateLogger<Heimatplatz.Api.Features.Locations.Data.Seeding.LocationSeeder>());
                await seeder.SeedAsync(ct);
                municipalities = await dbContext.Set<Municipality>().ToListAsync(ct);
                logger.LogInformation("Auto-seed erfolgreich: {Count} Municipalities geladen", municipalities.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Auto-seed der Municipalities fehlgeschlagen");
            }

            if (municipalities.Count == 0)
            {
                return new PropertySyncResult(0, 0, 0, 0, 1, ["Keine Municipalities vorhanden"]);
            }
        }

        // 3. Alle aktiven ForeclosureAuctions laden. IsActive bedeutet hier bereits "hat einen
        // gueltigen, in der Zukunft liegenden Versteigerungstermin und ist nicht nach Kategorie
        // ausgeschlossen" - ForeclosureAuctionSyncService.SyncAllAsync setzt abgeschlossene
        // Verfahren (Zuschlag/Meistbotsverteilung/Verschiebung ohne neuen Termin) bereits dort
        // auf IsActive=false. Ein zusaetzlicher Status-Text-Filter hier waere redundant.
        var activeAuctions = await dbContext.Set<ForeclosureAuction>()
            .Where(a => a.IsActive && a.ExternalId != null)
            .ToListAsync(ct);

        logger.LogInformation("Aktive Auctions fuer Property-Sync: {Count}", activeAuctions.Count);

        // 4. Bestehende Properties mit SourceName laden
        var existingProperties = await dbContext.Set<Property>()
            .Include(p => p.Contacts)
            .Where(p => p.SourceName == ForeclosureAuctionConstants.SourceName)
            .ToDictionaryAsync(p => p.SourceId!, ct);

        var now = DateTimeOffset.UtcNow;
        var processedSourceIds = new HashSet<string>();
        var newProperties = new List<(Property Property, string City)>();
        var geocodeCandidates = new List<(Property Property, ForeclosureAuction Auction)>();

        // 5. Upsert: Fuer jede aktive Auction → Property erstellen oder aktualisieren
        foreach (var auction in activeAuctions)
        {
            try
            {
                var sourceId = auction.ExternalId!;
                processedSourceIds.Add(sourceId);

                var municipalityId = ResolveMunicipalityId(auction.PostalCode, auction.City, municipalities);
                if (municipalityId == null)
                {
                    logger.LogWarning(
                        "Municipality nicht gefunden fuer PLZ={PostalCode}, Ort={City} - uebersprungen",
                        auction.PostalCode, auction.City);
                    skipped++;
                    continue;
                }

                var foreclosureData = BuildForeclosurePropertyData(auction);

                if (existingProperties.TryGetValue(sourceId, out var existingProperty))
                {
                    // Adressaenderung VOR der Mutation erkennen (UpdateProperty ueberschreibt sie)
                    if (existingProperty.Latitude == null || existingProperty.Address != auction.Address)
                        geocodeCandidates.Add((existingProperty, auction));

                    // Update
                    UpdateProperty(existingProperty, auction, municipalityId.Value, foreclosureData, now);
                    updated++;
                }
                else
                {
                    // Create
                    var property = CreateProperty(auction, systemUserId, municipalityId.Value, foreclosureData, now);
                    dbContext.Set<Property>().Add(property);
                    geocodeCandidates.Add((property, auction));

                    // Kontakt hinzufuegen
                    var contact = CreateContact(property, auction);
                    dbContext.Set<PropertyContactInfo>().Add(contact);

                    var municipalityName = municipalities.First(m => m.Id == municipalityId.Value).Name;
                    newProperties.Add((property, municipalityName));

                    created++;
                }
            }
            catch (Exception ex)
            {
                errors++;
                var msg = $"Fehler bei Auction {auction.ExternalId}: {ex.Message}";
                errorMessages.Add(msg);
                logger.LogWarning(ex, "Fehler beim Sync von Auction {ExternalId} zu Property", auction.ExternalId);
            }
        }

        // 6. Remove: Properties ohne aktive Auction loeschen. Admin-ausgeblendete Properties
        // bewusst NICHT loeschen: ein Hard-Delete+Recreate (z.B. verschobener Termin, kurzzeitig
        // fehlender Scrape) wuerde IsHidden=false zuruecksetzen, weil CreateProperty die Zeile
        // neu mit dem Default anlegt - eine Moderations-Entscheidung wuerde sich selbst aufheben.
        // Die Zeile bleibt stattdessen als ausgeblendeter Waisen-Datensatz liegen (fuer die
        // oeffentlichen Abfragen ohnehin unsichtbar) und wird beim naechsten Auftauchen des Edikts
        // ganz normal ueber den Update-Zweig weitergefuehrt.
        foreach (var (sourceId, property) in existingProperties)
        {
            if (!processedSourceIds.Contains(sourceId) && !property.IsHidden)
            {
                dbContext.Set<PropertyContactInfo>().RemoveRange(property.Contacts);
                dbContext.Set<Property>().Remove(property);
                removed++;
            }
        }

        // 6b. Koordinaten fuer neue bzw. adressgeaenderte Properties aufloesen -
        // Edikt-Adressen sind oeffentlich, punktgenaue Pins sind hier erwuenscht.
        // Gedeckelt pro Lauf, fehlertolerant (null = beim naechsten Lauf erneut).
        var geocodeAttempts = 0;
        foreach (var (property, auction) in geocodeCandidates)
        {
            if (geocodeAttempts >= MaxGeocodesPerSync)
                break;
            geocodeAttempts++;

            var geocodeResult = await propertyGeocoder.GeocodeAsync(
                auction.Address, auction.PostalCode, auction.City, ct);
            if (geocodeResult != null)
            {
                property.Latitude = geocodeResult.Latitude;
                property.Longitude = geocodeResult.Longitude;
                property.IsLocationExact = geocodeResult.IsExact;
            }
        }

        // 7. SaveChanges
        await dbContext.SaveChangesAsync(ct);

        // 8. Push-Benachrichtigungen fuer neue Properties
        foreach (var (property, city) in newProperties)
        {
            try
            {
                await mediator.Publish(new PropertyCreatedEvent(
                    property.Id,
                    property.Title,
                    city,
                    property.Price,
                    property.Type,
                    property.SellerType
                ), ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Push-Benachrichtigung fuer Property {PropertyId} fehlgeschlagen", property.Id);
            }
        }

        logger.LogInformation(
            "Property-Sync abgeschlossen: {Created} erstellt, {Updated} aktualisiert, {Removed} entfernt, {Skipped} uebersprungen, {Errors} Fehler",
            created, updated, removed, skipped, errors);

        return new PropertySyncResult(created, updated, removed, skipped, errors, errorMessages);
    }

    private static Guid? ResolveMunicipalityId(string postalCode, string city, List<Municipality> municipalities)
    {
        // Exakt nach PostalCode
        var match = municipalities.FirstOrDefault(m => m.PostalCode == postalCode);
        if (match != null) return match.Id;

        // Case-insensitive nach City-Name
        match = municipalities.FirstOrDefault(m =>
            string.Equals(m.Name, city, StringComparison.OrdinalIgnoreCase));
        if (match != null) return match.Id;

        // Partial-Match
        var cityLower = city.ToLowerInvariant();
        match = municipalities.FirstOrDefault(m =>
            m.Name.ToLowerInvariant().Contains(cityLower) || cityLower.Contains(m.Name.ToLowerInvariant()));
        if (match != null) return match.Id;

        return null;
    }

    private static Property CreateProperty(
        ForeclosureAuction auction,
        Guid systemUserId,
        Guid municipalityId,
        ForeclosurePropertyData foreclosureData,
        DateTimeOffset now)
    {
        var property = new Property
        {
            Id = Guid.NewGuid(),
            Title = BuildTitle(auction),
            Address = auction.Address,
            MunicipalityId = municipalityId,
            Price = auction.MinimumBid ?? auction.EstimatedValue ?? 0,
            LivingAreaSquareMeters = auction.BuildingArea.HasValue ? (int)auction.BuildingArea.Value : null,
            PlotAreaSquareMeters = (auction.TotalArea ?? auction.PlotArea).HasValue
                ? (int)(auction.TotalArea ?? auction.PlotArea)!.Value
                : null,
            Rooms = auction.NumberOfRooms,
            YearBuilt = auction.YearBuilt,
            Type = PropertyType.Foreclosure,
            SellerType = SellerType.Broker,
            SellerName = auction.Court ?? "Bezirksgericht",
            Description = auction.ObjectDescription,
            UserId = systemUserId,
            InquiryType = InquiryType.ContactData,
            ImageUrls = CollectImageUrls(auction),
            SourceName = ForeclosureAuctionConstants.SourceName,
            SourceId = auction.ExternalId,
            SourceUrl = auction.EdictUrl,
            SourceLastUpdated = auction.LastScrapedAt,
            CreatedAt = auction.PublicationDate ?? auction.FirstSeenAt ?? now
        };

        property.SetTypedData(foreclosureData);
        return property;
    }

    private static void UpdateProperty(
        Property property,
        ForeclosureAuction auction,
        Guid municipalityId,
        ForeclosurePropertyData foreclosureData,
        DateTimeOffset now)
    {
        property.Title = BuildTitle(auction);
        property.Address = auction.Address;
        property.MunicipalityId = municipalityId;
        property.Price = auction.MinimumBid ?? auction.EstimatedValue ?? 0;
        property.LivingAreaSquareMeters = auction.BuildingArea.HasValue ? (int)auction.BuildingArea.Value : null;
        property.PlotAreaSquareMeters = (auction.TotalArea ?? auction.PlotArea).HasValue
            ? (int)(auction.TotalArea ?? auction.PlotArea)!.Value
            : null;
        property.Rooms = auction.NumberOfRooms;
        property.YearBuilt = auction.YearBuilt;
        property.ImageUrls = CollectImageUrls(auction);
        property.SellerName = auction.Court ?? "Bezirksgericht";
        property.Description = auction.ObjectDescription;
        property.SourceUrl = auction.EdictUrl;
        property.SourceLastUpdated = auction.LastScrapedAt;

        // "Eingestellt am" zeigt die Bekanntmachung des Edikts, nicht den Scrape-Zeitpunkt.
        // Bei jedem Sync nachziehen, damit nachtraeglich ergaenzte PublicationDates auch auf
        // bestehenden Inseraten ankommen (bisher nur beim Anlegen bzw. per Backfill-Seeder).
        property.CreatedAt = auction.PublicationDate ?? auction.FirstSeenAt ?? property.CreatedAt;
        property.UpdatedAt = now;

        property.SetTypedData(foreclosureData);
    }

    /// <summary>
    /// Kurzer Anzeige-Titel statt der vollen ObjectDescription (die als Description verwendet wird) -
    /// sonst sind Titel und Beschreibung identisch (Bug: langer Fließtext als Kartentitel/Link-Name).
    /// </summary>
    private static string BuildTitle(ForeclosureAuction auction)
    {
        var categoryLabel = auction.Category switch
        {
            PropertyCategory.Einfamilienhaus => "Einfamilienhaus",
            PropertyCategory.Zweifamilienhaus => "Zweifamilienhaus",
            PropertyCategory.Mehrfamilienhaus => "Mehrfamilienhaus",
            PropertyCategory.Wohnungseigentum => "Wohnung",
            PropertyCategory.GewerblicheLiegenschaft => "Gewerbeobjekt",
            PropertyCategory.Grundstueck => "Grundstück",
            PropertyCategory.LandUndForstwirtschaft => "Land- und Forstwirtschaft",
            _ => "Liegenschaft"
        };

        return $"Zwangsversteigerung: {categoryLabel} in {auction.City}";
    }

    private static PropertyContactInfo CreateContact(Property property, ForeclosureAuction auction)
    {
        return new PropertyContactInfo
        {
            Id = Guid.NewGuid(),
            PropertyId = property.Id,
            Type = ContactType.Agent,
            Source = ContactSource.Import,
            Name = auction.Court ?? "Bezirksgericht",
            OriginalListingUrl = auction.EdictUrl,
            SourceName = ForeclosureAuctionConstants.SourceName,
            SourceId = auction.ExternalId,
            DisplayOrder = 0,
            CreatedAt = property.CreatedAt
        };
    }

    internal static ForeclosurePropertyData BuildForeclosurePropertyData(ForeclosureAuction auction)
    {
        // Status traegt den Edikt-Typ aus dem Seitentitel der Ediktsdatei
        // ("Versteigerung - Objekt 1", "Verschiebung", "Zuschlag mit Ueberbot - ...").
        // Terminale Typen erreichen den Sync normalerweise nicht (beim Scrapen deaktiviert),
        // werden aber der Vollstaendigkeit halber mit abgebildet.
        var statusText = auction.Status?.ToLowerInvariant() ?? "";
        var status = statusText switch
        {
            _ when statusText.StartsWith("versteigerung")
                || statusText.StartsWith("neuerliche versteigerung")
                || statusText.StartsWith("verschiebung") => LegalStatus.Scheduled,
            _ when statusText.StartsWith("zuschlag")
                || statusText.StartsWith("meistbotsverteilung") => LegalStatus.Completed,
            _ when statusText.StartsWith("entfall")
                || statusText.StartsWith("einstellung")
                || statusText.StartsWith("aufschiebung") => LegalStatus.Cancelled,
            _ => LegalStatus.Pending
        };

        return new ForeclosurePropertyData(
            CourtName: auction.Court ?? "Bezirksgericht",
            // UtcDateTime statt DateTime: DateTime wuerde Kind=Unspecified liefern und
            // beim JSON-Serialisieren das "Z" verlieren (live dadurch zwei Stunden zu frueh).
            AuctionDate: auction.AuctionDate.UtcDateTime,
            MinimumBid: auction.MinimumBid ?? 0,
            EstimatedValue: auction.EstimatedValue,
            Encumbrances: [],
            Status: status,
            FileNumber: auction.CaseNumber ?? "Unbekannt",
            RegistrationNumber: auction.RegistrationNumber,
            CadastralMunicipality: auction.CadastralMunicipality,
            PlotNumber: auction.PlotNumber,
            TotalArea: auction.TotalArea,
            BuildingArea: auction.BuildingArea,
            ZoningDesignation: auction.ZoningDesignation,
            BuildingCondition: auction.BuildingCondition,
            NumberOfRooms: auction.NumberOfRooms,
            YearBuilt: auction.YearBuilt,
            ViewingDate: auction.ViewingDate?.UtcDateTime,
            BiddingDeadline: auction.BiddingDeadline?.UtcDateTime,
            OwnershipShare: auction.OwnershipShare,
            Notes: auction.Notes,
            EdictUrl: auction.EdictUrl,
            FloorPlanUrl: auction.FloorPlanUrl,
            SitePlanUrl: auction.SitePlanUrl,
            LongAppraisalUrl: auction.LongAppraisalUrl,
            ShortAppraisalUrl: auction.ShortAppraisalUrl
        );
    }

    private static List<string> CollectImageUrls(ForeclosureAuction auction)
    {
        // Altbestand enthaelt Case-Duplikate (Direktlink vs. Thumbnail-abgeleitete URL),
        // daher case-insensitive dedupen
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var urls = new List<string>();

        // Scraped image URLs first (Fotos)
        foreach (var url in auction.ImageUrls)
        {
            if (!string.IsNullOrEmpty(url) && seen.Add(url))
                urls.Add(url);
        }

        // Add SitePlan/FloorPlan if not already included
        if (!string.IsNullOrEmpty(auction.SitePlanUrl) && seen.Add(auction.SitePlanUrl))
            urls.Add(auction.SitePlanUrl);
        if (!string.IsNullOrEmpty(auction.FloorPlanUrl) && seen.Add(auction.FloorPlanUrl))
            urls.Add(auction.FloorPlanUrl);

        return urls;
    }
}
