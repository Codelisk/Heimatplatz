using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Heimatplatz.Api.Features.ForeclosureAuctions.Data.Entities;
using Heimatplatz.Api.Features.Locations.Data.Entities;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Enums;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific.Enums;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Services;

public class ForeclosurePropertySyncService(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    ILogger<ForeclosurePropertySyncService> logger
) : IForeclosurePropertySyncService
{
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
                Vorname = "System",
                Nachname = "Heimatplatz",
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
            logger.LogWarning("Keine Municipalities in der Datenbank - Property-Sync wird uebersprungen");
            return new PropertySyncResult(0, 0, 0, 0, 1, ["Keine Municipalities vorhanden"]);
        }

        // 3. Alle aktiven ForeclosureAuctions laden
        var activeAuctions = await dbContext.Set<ForeclosureAuction>()
            .Where(a => a.IsActive && a.ExternalId != null)
            .ToListAsync(ct);

        // 4. Bestehende Properties mit SourceName laden
        var existingProperties = await dbContext.Set<Property>()
            .Include(p => p.Contacts)
            .Where(p => p.SourceName == ForeclosureAuctionConstants.SourceName)
            .ToDictionaryAsync(p => p.SourceId!, ct);

        var now = DateTimeOffset.UtcNow;
        var processedSourceIds = new HashSet<string>();

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
                    // Update
                    UpdateProperty(existingProperty, auction, municipalityId.Value, foreclosureData, now);
                    updated++;
                }
                else
                {
                    // Create
                    var property = CreateProperty(auction, systemUserId, municipalityId.Value, foreclosureData, now);
                    dbContext.Set<Property>().Add(property);

                    // Kontakt hinzufuegen
                    var contact = CreateContact(property, auction);
                    dbContext.Set<PropertyContactInfo>().Add(contact);

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

        // 6. Remove: Properties ohne aktive Auction loeschen
        foreach (var (sourceId, property) in existingProperties)
        {
            if (!processedSourceIds.Contains(sourceId))
            {
                dbContext.Set<PropertyContactInfo>().RemoveRange(property.Contacts);
                dbContext.Set<Property>().Remove(property);
                removed++;
            }
        }

        // 7. SaveChanges
        await dbContext.SaveChangesAsync(ct);

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
            Title = auction.ObjectDescription,
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
            SellerType = SellerType.Portal,
            SellerName = auction.Court ?? "Bezirksgericht",
            Description = auction.ObjectDescription,
            UserId = systemUserId,
            InquiryType = InquiryType.ContactData,
            SourceName = ForeclosureAuctionConstants.SourceName,
            SourceId = auction.ExternalId,
            SourceUrl = auction.EdictUrl,
            SourceLastUpdated = auction.LastScrapedAt,
            CreatedAt = now
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
        property.Title = auction.ObjectDescription;
        property.Address = auction.Address;
        property.MunicipalityId = municipalityId;
        property.Price = auction.MinimumBid ?? auction.EstimatedValue ?? 0;
        property.LivingAreaSquareMeters = auction.BuildingArea.HasValue ? (int)auction.BuildingArea.Value : null;
        property.PlotAreaSquareMeters = (auction.TotalArea ?? auction.PlotArea).HasValue
            ? (int)(auction.TotalArea ?? auction.PlotArea)!.Value
            : null;
        property.Rooms = auction.NumberOfRooms;
        property.YearBuilt = auction.YearBuilt;
        property.SellerName = auction.Court ?? "Bezirksgericht";
        property.Description = auction.ObjectDescription;
        property.SourceUrl = auction.EdictUrl;
        property.SourceLastUpdated = auction.LastScrapedAt;
        property.UpdatedAt = now;

        property.SetTypedData(foreclosureData);
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

    private static ForeclosurePropertyData BuildForeclosurePropertyData(ForeclosureAuction auction)
    {
        var status = auction.Status?.ToLowerInvariant() switch
        {
            "aktiv" => LegalStatus.Scheduled,
            "in bearbeitung" => LegalStatus.InProgress,
            "abgeschlossen" => LegalStatus.Completed,
            "storniert" or "aufgehoben" => LegalStatus.Cancelled,
            _ => LegalStatus.Pending
        };

        return new ForeclosurePropertyData(
            CourtName: auction.Court ?? "Bezirksgericht",
            AuctionDate: auction.AuctionDate.DateTime,
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
            ViewingDate: auction.ViewingDate?.DateTime,
            BiddingDeadline: auction.BiddingDeadline?.DateTime,
            OwnershipShare: auction.OwnershipShare,
            Notes: auction.Notes,
            EdictUrl: auction.EdictUrl,
            FloorPlanUrl: auction.FloorPlanUrl,
            SitePlanUrl: auction.SitePlanUrl,
            LongAppraisalUrl: auction.LongAppraisalUrl,
            ShortAppraisalUrl: auction.ShortAppraisalUrl
        );
    }
}
