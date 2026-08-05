using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Heimatplatz.Api.Features.Locations.Data.Entities;
using Heimatplatz.Api.Features.Notifications.Contracts.Events;
using Heimatplatz.Api.Features.OpenImmoImport.Configuration;
using Heimatplatz.Api.Features.OpenImmoImport.Models;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Enums;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific.Enums;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Heimatplatz.Api.Features.Properties.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.OpenImmoImport.Services;

public record OpenImmoSyncResult(
    int Created,
    int Updated,
    int Removed,
    int Unchanged,
    int Skipped,
    int Errors,
    List<string> ErrorMessages)
{
    /// <summary>
    /// True wenn der Lauf VOR dem Anwenden abgebrochen wurde (leerer Vollbestand,
    /// fehlende Municipalities) - der Orchestrator schreibt dann keinen Marker,
    /// der naechste Tick versucht es erneut.
    /// </summary>
    public bool Aborted { get; init; }

    public override string ToString()
        => $"{Created} neu, {Updated} aktualisiert, {Removed} entfernt, {Unchanged} unveraendert, "
           + $"{Skipped} uebersprungen, {Errors} Fehler";
}

public interface IOpenImmoPropertySyncService
{
    /// <summary>
    /// Synchronisiert einen geparsten OpenImmo-Vollbestand in die Property-Tabelle:
    /// Upsert ueber (SourceName, SourceId), Snapshot-Diff-Loeschung verschwundener
    /// Objekte (ausser IsHidden - Moderation ueberlebt), Bild-Materialisierung,
    /// Geocoding-Fallback und PropertyCreatedEvent je Neuzugang.
    /// </summary>
    Task<OpenImmoSyncResult> SyncAsync(
        OpenImmoFeedOptions feed,
        OpenImmoParseResult parseResult,
        IOpenImmoZipAccessor? zip,
        CancellationToken ct = default);
}

/// <summary>
/// Modelliert nach ForeclosurePropertySyncService. Wichtige Abweichung: Update-Laeufe
/// fassen unveraenderte Zeilen nicht an (EntityState-Check statt pauschalem
/// UpdatedAt=now) - bei mehreren Feed-Pushes pro Tag blieben sonst null echte
/// Aenderungen, aber jede Zeile wuerde je Lauf ein PropertyChange-Journal-Update
/// erzeugen (MAUI-Delta-Sync-Rauschen).
/// </summary>
public class OpenImmoPropertySyncService(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    ISellerInfoResolver sellerInfoResolver,
    IOpenImmoImageService imageService,
    IPropertyGeocoder propertyGeocoder,
    IOptions<OpenImmoImportOptions> options,
    IMediator mediator,
    ILogger<OpenImmoPropertySyncService> logger
) : IOpenImmoPropertySyncService
{
    public async Task<OpenImmoSyncResult> SyncAsync(
        OpenImmoFeedOptions feed,
        OpenImmoParseResult parseResult,
        IOpenImmoZipAccessor? zip,
        CancellationToken ct = default)
    {
        var created = 0;
        var updated = 0;
        var removed = 0;
        var unchanged = 0;
        var skipped = parseResult.SkippedCount;
        var errors = 0;
        var errorMessages = new List<string>();

        // Leerer Vollbestand = mutmasslich kaputter Push; ohne Guard wuerde der
        // Delete-Pass den kompletten Makler-Bestand loeschen (Analogon zum
        // Leerlisten-Guard des ZV-Syncs).
        if (parseResult.Listings.Count == 0 && !parseResult.IsPartialTransfer)
        {
            if (!options.Value.AllowEmptySnapshot)
            {
                return new OpenImmoSyncResult(0, 0, 0, 0, skipped, 1,
                    ["Feed enthaelt keine verwertbaren Objekte - Lauf abgebrochen (AllowEmptySnapshot=false)"])
                { Aborted = true };
            }

            logger.LogWarning(
                "[OpenImmoImport] Feed {FeedKey}: leerer Vollbestand wird wegen AllowEmptySnapshot=true angewendet",
                feed.Key);
        }

        // 1. System-User sicherstellen (gemeinsamer Import-User, gleiche GUID wie ZV-Sync)
        var systemUserId = OpenImmoImportConstants.SystemUserId;
        if (!await dbContext.Set<User>().AnyAsync(u => u.Id == systemUserId, ct))
        {
            logger.LogInformation("[OpenImmoImport] System-User existiert nicht, wird erstellt");
            dbContext.Set<User>().Add(new User
            {
                Id = systemUserId,
                FirstName = "System",
                LastName = "Heimatplatz",
                Email = "system@heimatplatz.at",
                PasswordHash = passwordHasher.Hash(Guid.NewGuid().ToString()),
                CreatedAt = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync(ct);
        }

        // 2. Municipalities fuer PLZ/Ort-Aufloesung (LocationSeeder laeuft bei jedem
        // API-Start - leer nur, wenn der Seed fehlschlug; naechster Lauf versucht es erneut)
        var municipalities = await dbContext.Set<Municipality>().ToListAsync(ct);
        if (municipalities.Count == 0)
        {
            return new OpenImmoSyncResult(0, 0, 0, 0, skipped, 1, ["Keine Municipalities vorhanden"])
            { Aborted = true };
        }

        // 3. SellerSource fuer den Anbieter-Ausschlussfilter (legt bei Bedarf an, speichert nicht)
        var sellerSourceId = await sellerInfoResolver.ResolveSellerSourceAsync(
            SellerType.Broker, feed.SellerName, ct);

        // 4. Bestand dieser Quelle laden
        var existingProperties = await dbContext.Set<Property>()
            .Include(p => p.Contacts)
            .Where(p => p.SourceName == feed.SourceName)
            .ToDictionaryAsync(p => p.SourceId!, ct);

        var now = DateTimeOffset.UtcNow;
        var processedSourceIds = new HashSet<string>();
        var newProperties = new List<(Property Property, string City)>();
        var geocodeCandidates = new List<(Property Property, OpenImmoListing Listing)>();

        // 5. Upsert
        foreach (var listing in parseResult.Listings)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                // VOR der Municipality-Aufloesung registrieren: ein transient nicht
                // aufloesbares Objekt darf im Delete-Pass nicht als "verschwunden"
                // gelten und den Bestand loeschen (ZV-Reihenfolge, bewusst kopiert)
                processedSourceIds.Add(listing.SourceId);

                var municipalityId = ResolveMunicipalityId(listing.PostalCode, listing.City, municipalities);
                if (municipalityId == null)
                {
                    logger.LogWarning(
                        "[OpenImmoImport] Municipality nicht gefunden fuer PLZ={PostalCode}, Ort={City} - uebersprungen",
                        listing.PostalCode, listing.City);
                    skipped++;
                    continue;
                }

                var imageUrls = await imageService.MaterializeAsync(feed, listing, zip, ct);

                if (existingProperties.TryGetValue(listing.SourceId, out var existingProperty))
                {
                    // Adressaenderung VOR der Mutation erkennen (ApplyListing ueberschreibt sie)
                    var addressChanged = existingProperty.Address != BuildAddress(listing);
                    ApplyListing(existingProperty, listing, municipalityId.Value, sellerSourceId, feed, imageUrls);
                    var contactsChanged = SyncContact(existingProperty, listing, feed);

                    if (listing.Latitude.HasValue && listing.Longitude.HasValue)
                    {
                        ApplyFeedCoordinates(existingProperty, listing);
                    }
                    else if (existingProperty.Latitude == null || addressChanged)
                    {
                        geocodeCandidates.Add((existingProperty, listing));
                    }

                    if (dbContext.Entry(existingProperty).State == EntityState.Modified || contactsChanged)
                    {
                        existingProperty.UpdatedAt = now;
                        updated++;
                    }
                    else
                    {
                        unchanged++;
                    }
                }
                else
                {
                    var property = CreateProperty(listing, systemUserId, municipalityId.Value, sellerSourceId, feed, imageUrls, now);
                    dbContext.Set<Property>().Add(property);

                    if (listing.Latitude.HasValue && listing.Longitude.HasValue)
                        ApplyFeedCoordinates(property, listing);
                    else
                        geocodeCandidates.Add((property, listing));

                    dbContext.Set<PropertyContactInfo>().Add(CreateContact(property, listing, feed));

                    var municipalityName = municipalities.First(m => m.Id == municipalityId.Value).Name;
                    newProperties.Add((property, municipalityName));
                    created++;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                errors++;
                errorMessages.Add($"Fehler bei Objekt {listing.SourceId}: {ex.Message}");
                logger.LogWarning(ex, "[OpenImmoImport] Fehler beim Sync von Objekt {SourceId}", listing.SourceId);
            }
        }

        // 6. Delete-Pass: verschwundene Objekte entfernen. Nur bei Vollbestand -
        // TEIL-Uebertragungen loeschen ausschliesslich explizit markierte Objekte.
        // IsHidden-Zeilen bleiben bewusst liegen (Moderation ueberlebt, siehe ZV-Sync).
        var explicitDeletes = new HashSet<string>(parseResult.DeletedSourceIds);
        foreach (var (sourceId, property) in existingProperties)
        {
            var vanished = !parseResult.IsPartialTransfer && !processedSourceIds.Contains(sourceId);
            if ((vanished || explicitDeletes.Contains(sourceId)) && !property.IsHidden)
            {
                dbContext.Set<PropertyContactInfo>().RemoveRange(property.Contacts);
                dbContext.Set<Property>().Remove(property);
                imageService.DeleteListingImages(feed.Key, sourceId);
                removed++;
            }
        }

        // 7. Geocoding-Fallback nur fuer Objekte ohne Feed-Koordinaten (Nominatim,
        // 1 Request/Sekunde gedrosselt, daher pro Lauf gedeckelt; Rest beim naechsten Lauf)
        var geocodeAttempts = 0;
        foreach (var (property, listing) in geocodeCandidates)
        {
            if (geocodeAttempts >= options.Value.MaxGeocodesPerRun)
                break;
            geocodeAttempts++;

            var geocodeResult = await propertyGeocoder.GeocodeAsync(
                listing.Street, listing.PostalCode, listing.City, ct);
            if (geocodeResult != null)
            {
                property.Latitude = geocodeResult.Latitude;
                property.Longitude = geocodeResult.Longitude;
                property.IsLocationExact = geocodeResult.IsExact;
            }
        }

        // 8. Ein SaveChanges fuer den ganzen Lauf (PropertyChangeInterceptor journalt
        // getrackte Aenderungen automatisch fuer den MAUI-Delta-Sync)
        await dbContext.SaveChangesAsync(ct);

        // 9. Push-Benachrichtigungen fuer Neuzugaenge
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
                logger.LogWarning(ex,
                    "[OpenImmoImport] Push-Benachrichtigung fuer Property {PropertyId} fehlgeschlagen", property.Id);
            }
        }

        var result = new OpenImmoSyncResult(created, updated, removed, unchanged, skipped, errors, errorMessages);
        logger.LogInformation("[OpenImmoImport] Feed {FeedKey}: {Result}", feed.Key, result);
        return result;
    }

    /// <summary>
    /// Aufloesung nach ZV-Vorbild, aber mit PLZ+Name-Vorstufe: mehrere Gemeinden
    /// teilen sich eine PLZ (4600 = Wels UND Thalheim bei Wels) - der reine
    /// PLZ-Match wuerde die erstbeste Gemeinde nehmen. Makler-Feeds liefern den
    /// Ort verlaesslich mit, also zuerst die Kombination pruefen.
    /// </summary>
    internal static Guid? ResolveMunicipalityId(string postalCode, string city, List<Municipality> municipalities)
    {
        var match = municipalities.FirstOrDefault(m =>
            m.PostalCode == postalCode && string.Equals(m.Name, city, StringComparison.OrdinalIgnoreCase));
        if (match != null) return match.Id;

        match = municipalities.FirstOrDefault(m => m.PostalCode == postalCode);
        if (match != null) return match.Id;

        if (string.IsNullOrWhiteSpace(city))
            return null;

        match = municipalities.FirstOrDefault(m =>
            string.Equals(m.Name, city, StringComparison.OrdinalIgnoreCase));
        if (match != null) return match.Id;

        var cityLower = city.ToLowerInvariant();
        match = municipalities.FirstOrDefault(m =>
            m.Name.ToLowerInvariant().Contains(cityLower) || cityLower.Contains(m.Name.ToLowerInvariant()));
        return match?.Id;
    }

    private static string BuildAddress(OpenImmoListing listing)
        => listing.AddressReleased && listing.Street != null ? listing.Street : "";

    private static Property CreateProperty(
        OpenImmoListing listing,
        Guid systemUserId,
        Guid municipalityId,
        Guid? sellerSourceId,
        OpenImmoFeedOptions feed,
        List<string> imageUrls,
        DateTimeOffset now)
    {
        var property = new Property
        {
            Id = Guid.NewGuid(),
            Title = listing.Title,
            Address = BuildAddress(listing),
            MunicipalityId = municipalityId,
            PostalCode = string.IsNullOrWhiteSpace(listing.PostalCode) ? null : listing.PostalCode,
            Price = listing.Price,
            LivingAreaSquareMeters = listing.LivingAreaSquareMeters,
            PlotAreaSquareMeters = listing.PlotAreaSquareMeters,
            Rooms = listing.Rooms,
            YearBuilt = listing.YearBuilt,
            IsNewBuildProject = listing.IsNewBuildProject,
            Type = listing.Type,
            SellerType = SellerType.Broker,
            SellerName = feed.SellerName,
            SellerSourceId = sellerSourceId,
            Description = listing.Description,
            Features = listing.Features.ToList(),
            ImageUrls = imageUrls,
            UserId = systemUserId,
            InquiryType = InquiryType.ContactData,
            LocationDisplay = listing.AddressReleased ? LocationDisplayMode.Exact : LocationDisplayMode.Approximate,
            SourceName = feed.SourceName,
            SourceId = listing.SourceId,
            SourceUrl = listing.ExternalUrl,
            SourceLastUpdated = listing.StandVom,
            CreatedAt = now
        };

        property.TypeSpecificData = BuildTypeSpecificJson(listing);
        return property;
    }

    /// <summary>
    /// Wendet alle Feed-Felder auf eine Bestandszeile an. KEIN UpdatedAt hier -
    /// das setzt der Aufrufer nur, wenn EF tatsaechlich Aenderungen erkannt hat.
    /// </summary>
    private static void ApplyListing(
        Property property,
        OpenImmoListing listing,
        Guid municipalityId,
        Guid? sellerSourceId,
        OpenImmoFeedOptions feed,
        List<string> imageUrls)
    {
        property.Title = listing.Title;
        property.Address = BuildAddress(listing);
        property.MunicipalityId = municipalityId;
        property.PostalCode = string.IsNullOrWhiteSpace(listing.PostalCode) ? null : listing.PostalCode;
        property.Price = listing.Price;
        property.LivingAreaSquareMeters = listing.LivingAreaSquareMeters;
        property.PlotAreaSquareMeters = listing.PlotAreaSquareMeters;
        property.Rooms = listing.Rooms;
        property.YearBuilt = listing.YearBuilt;
        property.IsNewBuildProject = listing.IsNewBuildProject;
        property.Type = listing.Type;
        property.SellerType = SellerType.Broker;
        property.SellerName = feed.SellerName;
        property.SellerSourceId = sellerSourceId;
        property.Description = listing.Description;

        // Listen nur bei inhaltlicher Aenderung neu zuweisen: die JSON-Spalten haben
        // keinen ValueComparer (Referenzvergleich) - eine frische Liste pro Lauf wuerde
        // die Zeile immer als Modified markieren und die No-Op-Erkennung aushebeln
        if (!property.Features.SequenceEqual(listing.Features))
            property.Features = listing.Features.ToList();
        if (!property.ImageUrls.SequenceEqual(imageUrls))
            property.ImageUrls = imageUrls;
        property.LocationDisplay = listing.AddressReleased ? LocationDisplayMode.Exact : LocationDisplayMode.Approximate;
        property.SourceUrl = listing.ExternalUrl;
        property.SourceLastUpdated = listing.StandVom;
        property.TypeSpecificData = BuildTypeSpecificJson(listing);
    }

    /// <summary>Koordinaten aus dem Feed sind punktgenau (Makler-Software).</summary>
    private static void ApplyFeedCoordinates(Property property, OpenImmoListing listing)
    {
        property.Latitude = listing.Latitude;
        property.Longitude = listing.Longitude;
        property.IsLocationExact = true;
    }

    internal static string BuildTypeSpecificJson(OpenImmoListing listing)
    {
        if (listing.Type == PropertyType.House)
        {
            return System.Text.Json.JsonSerializer.Serialize(new HousePropertyData(
                LivingAreaInSquareMeters: listing.LivingAreaSquareMeters ?? 0,
                TotalRooms: listing.Rooms ?? 0,
                Bedrooms: listing.Bedrooms ?? 0,
                Bathrooms: listing.Bathrooms ?? 0,
                YearBuilt: listing.YearBuilt,
                Floors: listing.Floors ?? 1,
                Condition: listing.Condition ?? PropertyCondition.Average,
                HasGarage: listing.HasGarage,
                HasGarden: listing.HasGarden,
                HasBasement: listing.HasBasement));
        }

        var zoning = listing.Zoning ?? ZoningType.Residential;
        return System.Text.Json.JsonSerializer.Serialize(new LandPropertyData(
            PlotSizeInSquareMeters: listing.PlotAreaSquareMeters ?? 0,
            Zoning: zoning,
            HasBuildingRights: zoning is ZoningType.Residential or ZoningType.Commercial or ZoningType.Mixed,
            SoilQuality: null,
            IsBuildable: zoning == ZoningType.Residential));
    }

    private static PropertyContactInfo CreateContact(Property property, OpenImmoListing listing, OpenImmoFeedOptions feed)
    {
        return new PropertyContactInfo
        {
            Id = Guid.NewGuid(),
            PropertyId = property.Id,
            Type = ContactType.Agent,
            Source = ContactSource.Import,
            Name = listing.Contact?.Name ?? feed.SellerName,
            Email = listing.Contact?.Email,
            Phone = listing.Contact?.Phone,
            OriginalListingUrl = listing.ExternalUrl,
            SourceName = feed.SourceName,
            SourceId = listing.SourceId,
            DisplayOrder = 0,
            CreatedAt = property.CreatedAt
        };
    }

    /// <summary>
    /// Ersetzt den Import-Kontakt nur bei tatsaechlicher Aenderung - sonst wuerde
    /// jeder Lauf via PropertyChangeInterceptor ein Journal-Update ausloesen.
    /// </summary>
    private bool SyncContact(Property property, OpenImmoListing listing, OpenImmoFeedOptions feed)
    {
        var desiredName = listing.Contact?.Name ?? feed.SellerName;
        var desiredEmail = listing.Contact?.Email;
        var desiredPhone = listing.Contact?.Phone;

        var existing = property.Contacts.FirstOrDefault(c => c.Source == ContactSource.Import);
        if (existing != null
            && property.Contacts.Count == 1
            && existing.Name == desiredName
            && existing.Email == desiredEmail
            && existing.Phone == desiredPhone
            && existing.OriginalListingUrl == listing.ExternalUrl)
        {
            return false;
        }

        dbContext.Set<PropertyContactInfo>().RemoveRange(property.Contacts);
        property.Contacts.Clear();

        var contact = CreateContact(property, listing, feed);
        dbContext.Set<PropertyContactInfo>().Add(contact);
        property.Contacts.Add(contact);
        return true;
    }
}
