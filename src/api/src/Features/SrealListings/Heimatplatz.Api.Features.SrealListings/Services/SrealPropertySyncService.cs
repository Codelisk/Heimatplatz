using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Heimatplatz.Api.Features.ForeclosureAuctions.Services;
using Heimatplatz.Api.Features.Locations.Data.Entities;
using Heimatplatz.Api.Features.Notifications.Contracts.Events;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Enums;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific.Enums;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Heimatplatz.Api.Features.SrealListings.Contracts;
using Heimatplatz.Api.Features.SrealListings.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.SrealListings.Services;

public class SrealPropertySyncService(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IHttpClientFactory httpClientFactory,
    ILoggerFactory loggerFactory,
    IMediator mediator,
    ILogger<SrealPropertySyncService> logger
) : ISrealPropertySyncService
{
    public async Task<SrealPropertySyncResult> SyncToPropertiesAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starte Property-Sync aus sreal.at Listings");

        var created = 0;
        var updated = 0;
        var removed = 0;
        var skipped = 0;
        var errors = 0;
        var errorMessages = new List<string>();

        // 1. System-User sicherstellen (gleicher wie ForeclosureAuctions)
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

        // 2. Municipalities laden
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
                return new SrealPropertySyncResult(0, 0, 0, 0, 1, ["Keine Municipalities vorhanden"]);
        }

        // 3. Alle aktiven SrealListings laden
        var activeListings = await dbContext.Set<SrealListing>()
            .Where(s => s.IsActive)
            .ToListAsync(ct);

        // 4. Bestehende Properties mit SourceName laden
        var existingProperties = await dbContext.Set<Property>()
            .Include(p => p.Contacts)
            .Where(p => p.SourceName == SrealListingConstants.SourceName)
            .ToDictionaryAsync(p => p.SourceId!, ct);

        var now = DateTimeOffset.UtcNow;
        var processedSourceIds = new HashSet<string>();
        var newProperties = new List<(Property Property, string City)>();

        // 5. Upsert: Fuer jedes aktive Listing → Property erstellen oder aktualisieren
        foreach (var listing in activeListings)
        {
            try
            {
                var sourceId = listing.ExternalId;
                processedSourceIds.Add(sourceId);

                var municipalityId = ResolveMunicipalityId(listing.PostalCode, listing.City, municipalities);
                if (municipalityId == null)
                {
                    logger.LogWarning(
                        "Municipality nicht gefunden fuer PLZ={PostalCode}, Ort={City} - uebersprungen",
                        listing.PostalCode, listing.City);
                    skipped++;
                    continue;
                }

                if (existingProperties.TryGetValue(sourceId, out var existingProperty))
                {
                    UpdateProperty(existingProperty, listing, municipalityId.Value, now);
                    updated++;
                }
                else
                {
                    var property = CreateProperty(listing, systemUserId, municipalityId.Value, now);
                    dbContext.Set<Property>().Add(property);

                    var contact = CreateContact(property, listing);
                    dbContext.Set<PropertyContactInfo>().Add(contact);

                    var municipalityName = municipalities.First(m => m.Id == municipalityId.Value).Name;
                    newProperties.Add((property, municipalityName));

                    created++;
                }
            }
            catch (Exception ex)
            {
                errors++;
                var msg = $"Fehler bei sreal.at Listing {listing.ExternalId}: {ex.Message}";
                errorMessages.Add(msg);
                logger.LogWarning(ex, "Fehler beim Sync von sreal.at Listing {ExternalId} zu Property", listing.ExternalId);
            }
        }

        // 6. Remove: Properties ohne aktives Listing loeschen
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
            "sreal.at Property-Sync abgeschlossen: {Created} erstellt, {Updated} aktualisiert, {Removed} entfernt, {Skipped} uebersprungen, {Errors} Fehler",
            created, updated, removed, skipped, errors);

        return new SrealPropertySyncResult(created, updated, removed, skipped, errors, errorMessages);
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
        SrealListing listing,
        Guid systemUserId,
        Guid municipalityId,
        DateTimeOffset now)
    {
        var propertyType = listing.ObjectType switch
        {
            SrealObjectType.Land => PropertyType.Land,
            _ => PropertyType.House
        };

        var property = new Property
        {
            Id = Guid.NewGuid(),
            Title = listing.Title,
            Address = listing.Address,
            MunicipalityId = municipalityId,
            Price = listing.Price ?? 0,
            LivingAreaSquareMeters = listing.LivingArea.HasValue ? (int)listing.LivingArea.Value : null,
            PlotAreaSquareMeters = listing.PlotArea.HasValue ? (int)listing.PlotArea.Value : null,
            Rooms = listing.Rooms,
            Type = propertyType,
            SellerType = SellerType.Portal,
            SellerName = listing.AgentName ?? "s REAL",
            Description = listing.Description?.Length > 4000 ? listing.Description[..4000] : listing.Description,
            UserId = systemUserId,
            InquiryType = InquiryType.ContactData,
            ImageUrls = listing.ImageUrls,
            SourceName = SrealListingConstants.SourceName,
            SourceId = listing.ExternalId,
            SourceUrl = listing.SourceUrl,
            SourceLastUpdated = listing.LastScrapedAt,
            CreatedAt = listing.FirstSeenAt ?? now
        };

        // Set type-specific data
        if (propertyType == PropertyType.Land)
        {
            property.SetTypedData(new LandPropertyData(
                PlotSizeInSquareMeters: listing.PlotArea ?? 0,
                Zoning: ZoningType.Residential,
                HasBuildingRights: true,
                SoilQuality: null,
                IsBuildable: true,
                HasUtilities: false
            ));
        }
        else
        {
            property.SetTypedData(new HousePropertyData(
                LivingAreaInSquareMeters: listing.LivingArea ?? 0,
                TotalRooms: listing.Rooms ?? 0,
                Bedrooms: 0,
                Bathrooms: 0,
                YearBuilt: null,
                Floors: 0,
                Condition: PropertyCondition.Average,
                HasGarage: false,
                HasGarden: listing.PlotArea > 0,
                HasBasement: false
            ));
        }

        return property;
    }

    private static void UpdateProperty(
        Property property,
        SrealListing listing,
        Guid municipalityId,
        DateTimeOffset now)
    {
        property.Title = listing.Title;
        property.Address = listing.Address;
        property.MunicipalityId = municipalityId;
        property.Price = listing.Price ?? 0;
        property.LivingAreaSquareMeters = listing.LivingArea.HasValue ? (int)listing.LivingArea.Value : null;
        property.PlotAreaSquareMeters = listing.PlotArea.HasValue ? (int)listing.PlotArea.Value : null;
        property.Rooms = listing.Rooms;
        property.ImageUrls = listing.ImageUrls;
        property.SellerName = listing.AgentName ?? "s REAL";
        property.Description = listing.Description;
        property.SourceUrl = listing.SourceUrl;
        property.SourceLastUpdated = listing.LastScrapedAt;
        property.UpdatedAt = now;
    }

    private static PropertyContactInfo CreateContact(Property property, SrealListing listing)
    {
        return new PropertyContactInfo
        {
            Id = Guid.NewGuid(),
            PropertyId = property.Id,
            Type = ContactType.Agent,
            Source = ContactSource.Import,
            Name = listing.AgentName ?? "s REAL",
            Email = listing.AgentEmail,
            Phone = listing.AgentPhone,
            OriginalListingUrl = listing.SourceUrl,
            SourceName = SrealListingConstants.SourceName,
            SourceId = listing.ExternalId,
            DisplayOrder = 0,
            CreatedAt = property.CreatedAt
        };
    }
}
