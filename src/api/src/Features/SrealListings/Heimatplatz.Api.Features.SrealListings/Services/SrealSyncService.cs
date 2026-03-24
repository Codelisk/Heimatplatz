using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts;
using Heimatplatz.Api.Features.Locations.Data.Entities;
using Heimatplatz.Api.Features.SrealListings.Contracts;
using Heimatplatz.Api.Features.SrealListings.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.SrealListings.Services;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class SrealSyncService(
    ISrealScraper scraper,
    AppDbContext dbContext,
    ISrealPropertySyncService propertySyncService,
    ILogger<SrealSyncService> logger
) : ISrealSyncService
{
    public async Task<SrealSyncResult> SyncAllAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starte sreal.at Sync");

        var created = 0;
        var updated = 0;
        var removed = 0;
        var unchanged = 0;
        var errors = 0;
        var errorMessages = new List<string>();

        // 1. Alle Inserate von sreal.at holen
        List<SrealListItem> listItems;
        try
        {
            listItems = await scraper.GetListingsAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Abrufen der sreal.at Liste");
            return new SrealSyncResult(0, 0, 0, 0, 1, [$"Fehler beim Abrufen der Liste: {ex.Message}"]);
        }

        var now = DateTimeOffset.UtcNow;
        var scrapedExternalIds = new HashSet<string>();

        // 2. Bestehende Eintraege laden
        Dictionary<string, SrealListing> existingListings;
        try
        {
            existingListings = await dbContext.Set<SrealListing>()
                .ToDictionaryAsync(s => s.ExternalId, ct);
        }
        catch (Exception ex) when (ex.Message.Contains("no such column") || ex.InnerException?.Message?.Contains("no such column") == true
                                   || ex.Message.Contains("no such table") || ex.InnerException?.Message?.Contains("no such table") == true
                                   || ex.Message.Contains("Invalid object name") || ex.InnerException?.Message?.Contains("Invalid object name") == true)
        {
            logger.LogWarning(ex, "Schema mismatch detected - applying schema fix");
            // Tabellen fehlen: SQL direkt ausfuehren fuer die neuen Tabellen
            try
            {
                await dbContext.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SrealListings')
                    BEGIN
                        CREATE TABLE [SrealListings] (
                            [Id] uniqueidentifier NOT NULL,
                            [ExternalId] nvarchar(50) NOT NULL,
                            [Title] nvarchar(500) NOT NULL,
                            [Address] nvarchar(500) NOT NULL,
                            [City] nvarchar(100) NOT NULL,
                            [PostalCode] nvarchar(10) NOT NULL,
                            [District] nvarchar(100) NULL,
                            [State] int NOT NULL DEFAULT 4,
                            [ObjectType] int NOT NULL,
                            [BuyingType] nvarchar(10) NULL,
                            [Price] decimal(12,2) NULL,
                            [PriceText] nvarchar(100) NULL,
                            [Commission] nvarchar(200) NULL,
                            [LivingArea] decimal(12,2) NULL,
                            [PlotArea] decimal(12,2) NULL,
                            [Rooms] int NULL,
                            [Description] nvarchar(max) NULL,
                            [EnergyClass] nvarchar(5) NULL,
                            [EnergyValue] nvarchar(50) NULL,
                            [FGee] nvarchar(20) NULL,
                            [FGeeClass] nvarchar(5) NULL,
                            [ImageUrls] nvarchar(max) NULL,
                            [SourceUrl] nvarchar(1000) NOT NULL,
                            [AgentName] nvarchar(200) NULL,
                            [AgentPhone] nvarchar(50) NULL,
                            [AgentEmail] nvarchar(200) NULL,
                            [AgentOffice] nvarchar(200) NULL,
                            [Infrastructure] nvarchar(4000) NULL,
                            [ContentHash] nvarchar(64) NULL,
                            [IsActive] bit NOT NULL DEFAULT 1,
                            [FirstSeenAt] datetimeoffset NULL,
                            [LastScrapedAt] datetimeoffset NULL,
                            [RemovedAt] datetimeoffset NULL,
                            [CreatedAt] datetimeoffset NOT NULL,
                            [UpdatedAt] datetimeoffset NULL,
                            CONSTRAINT [PK_SrealListings] PRIMARY KEY ([Id])
                        );
                        CREATE UNIQUE INDEX [IX_SrealListings_ExternalId] ON [SrealListings] ([ExternalId]);
                        CREATE INDEX [IX_SrealListings_City] ON [SrealListings] ([City]);
                        CREATE INDEX [IX_SrealListings_PostalCode] ON [SrealListings] ([PostalCode]);
                        CREATE INDEX [IX_SrealListings_IsActive] ON [SrealListings] ([IsActive]);
                    END;
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SrealListingChanges')
                    BEGIN
                        CREATE TABLE [SrealListingChanges] (
                            [Id] uniqueidentifier NOT NULL,
                            [SrealListingId] uniqueidentifier NOT NULL,
                            [ChangeType] nvarchar(20) NOT NULL,
                            [ChangedFields] nvarchar(4000) NULL,
                            [OldContentHash] nvarchar(64) NULL,
                            [NewContentHash] nvarchar(64) NULL,
                            [CreatedAt] datetimeoffset NOT NULL,
                            [UpdatedAt] datetimeoffset NULL,
                            CONSTRAINT [PK_SrealListingChanges] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_SrealListingChanges_SrealListings] FOREIGN KEY ([SrealListingId]) REFERENCES [SrealListings] ([Id]) ON DELETE CASCADE
                        );
                        CREATE INDEX [IX_SrealListingChanges_SrealListingId] ON [SrealListingChanges] ([SrealListingId]);
                        CREATE INDEX [IX_SrealListingChanges_ChangeType] ON [SrealListingChanges] ([ChangeType]);
                        CREATE INDEX [IX_SrealListingChanges_CreatedAt] ON [SrealListingChanges] ([CreatedAt]);
                    END;
                    """, ct);
                logger.LogInformation("SrealListings tables created via raw SQL");
            }
            catch (Exception createEx)
            {
                logger.LogWarning(createEx, "Raw SQL table creation failed, falling back to EnsureDeleted/EnsureCreated");
                await dbContext.Database.EnsureDeletedAsync(ct);
                await dbContext.Database.EnsureCreatedAsync(ct);
            }
            existingListings = new Dictionary<string, SrealListing>();
        }

        // 3. Fuer jeden Listeneintrag die Details holen und verarbeiten
        foreach (var item in listItems)
        {
            try
            {
                scrapedExternalIds.Add(item.ExternalId);

                var detail = await scraper.GetListingDetailAsync(item.DetailUrl, ct);
                var contentHash = ComputeContentHash(detail.AllFields);

                if (existingListings.TryGetValue(item.ExternalId, out var existing))
                {
                    // Existiert bereits - pruefen ob sich etwas geaendert hat
                    if (existing.ContentHash == contentHash)
                    {
                        existing.LastScrapedAt = now;

                        if (!existing.IsActive)
                        {
                            existing.IsActive = true;
                            existing.RemovedAt = null;
                            LogChange(existing, "Reappeared", existing.ContentHash, contentHash);
                            updated++;
                        }
                        else
                        {
                            unchanged++;
                        }
                    }
                    else
                    {
                        var changedFields = DetectChangedFields(existing, detail);
                        UpdateEntityFromDetail(existing, detail, item);
                        existing.ContentHash = contentHash;
                        existing.LastScrapedAt = now;
                        existing.IsActive = true;
                        existing.RemovedAt = null;
                        LogChange(existing, "Updated", existing.ContentHash, contentHash, changedFields);
                        updated++;
                    }
                }
                else
                {
                    var listing = CreateEntityFromDetail(detail, item, contentHash, now);
                    dbContext.Set<SrealListing>().Add(listing);
                    LogChange(listing, "Created", null, contentHash);
                    created++;
                }
            }
            catch (Exception ex)
            {
                errors++;
                var msg = $"Fehler bei sreal.at Inserat {item.ExternalId}: {ex.Message}";
                errorMessages.Add(msg);
                logger.LogWarning(ex, "Fehler beim Verarbeiten von sreal.at Inserat {ExternalId}", item.ExternalId);
            }
        }

        // 4. Entfernte Eintraege markieren
        foreach (var existing in existingListings.Values)
        {
            if (existing.IsActive && !scrapedExternalIds.Contains(existing.ExternalId))
            {
                existing.IsActive = false;
                existing.RemovedAt = now;
                LogChange(existing, "Removed", existing.ContentHash, null);
                removed++;
            }
        }

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex.InnerException?.Message?.Contains("truncated") == true
                                   || ex.Message.Contains("truncated"))
        {
            logger.LogWarning(ex, "Data truncation detected - recreating database and retrying sync");
            foreach (var entry in dbContext.ChangeTracker.Entries().ToList())
                entry.State = EntityState.Detached;

            await dbContext.Database.EnsureDeletedAsync(ct);
            await dbContext.Database.EnsureCreatedAsync(ct);

            return await SyncAllAsync(ct);
        }

        // 5. District-Aufloesung per PLZ → Municipality → District
        try
        {
            var listingsWithoutDistrict = await dbContext.Set<SrealListing>()
                .Where(s => s.IsActive && s.District == null)
                .ToListAsync(ct);

            if (listingsWithoutDistrict.Count > 0)
            {
                var municipalities = await dbContext.Set<Municipality>()
                    .Include(m => m.District)
                    .ToListAsync(ct);

                foreach (var listing in listingsWithoutDistrict)
                {
                    var muni = municipalities.FirstOrDefault(m => m.PostalCode == listing.PostalCode)
                        ?? municipalities.FirstOrDefault(m =>
                            string.Equals(m.Name, listing.City, StringComparison.OrdinalIgnoreCase));

                    if (muni?.District != null)
                        listing.District = muni.District.Name;
                }

                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation(
                    "District-Aufloesung: {Resolved}/{Total} ohne District aufgeloest",
                    listingsWithoutDistrict.Count(l => l.District != null),
                    listingsWithoutDistrict.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "District-Aufloesung fehlgeschlagen (nicht kritisch)");
        }

        // 6. Properties synchronisieren
        try
        {
            var propertySyncResult = await propertySyncService.SyncToPropertiesAsync(ct);
            logger.LogInformation(
                "Property-Sync: {Created} erstellt, {Updated} aktualisiert, {Removed} entfernt, {Skipped} uebersprungen",
                propertySyncResult.Created, propertySyncResult.Updated,
                propertySyncResult.Removed, propertySyncResult.Skipped);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Property-Sync nach sreal.at Sync");
            errorMessages.Add($"Property-Sync fehlgeschlagen: {ex.Message} | Inner: {ex.InnerException?.Message}");
            errors++;
        }

        logger.LogInformation(
            "sreal.at Sync abgeschlossen: {Created} neu, {Updated} aktualisiert, {Removed} entfernt, {Unchanged} unveraendert, {Errors} Fehler",
            created, updated, removed, unchanged, errors);

        return new SrealSyncResult(created, updated, removed, unchanged, errors, errorMessages);
    }

    private SrealListing CreateEntityFromDetail(SrealDetail detail, SrealListItem listItem, string contentHash, DateTimeOffset now)
    {
        return new SrealListing
        {
            ExternalId = detail.ExternalId,
            Title = detail.Title.Length > 500 ? detail.Title[..500] : detail.Title,
            Address = detail.Address ?? listItem.Address ?? "Unbekannt",
            City = detail.City ?? listItem.City ?? "Unbekannt",
            PostalCode = detail.PostalCode ?? listItem.PostalCode ?? "0000",
            District = detail.District,
            State = AustrianState.Oberoesterreich,
            ObjectType = detail.ObjectType,
            BuyingType = detail.BuyingType ?? "buy",
            Price = detail.Price,
            PriceText = detail.PriceText,
            Commission = detail.Commission,
            LivingArea = detail.LivingArea,
            PlotArea = detail.PlotArea,
            Rooms = detail.Rooms,
            Description = detail.Description,
            EnergyClass = detail.EnergyClass,
            EnergyValue = detail.EnergyValue,
            FGee = detail.FGee,
            FGeeClass = detail.FGeeClass,
            ImageUrls = detail.ImageUrls,
            SourceUrl = detail.SourceUrl,
            AgentName = detail.AgentName,
            AgentPhone = detail.AgentPhone,
            AgentEmail = detail.AgentEmail,
            AgentOffice = detail.AgentOffice,
            Infrastructure = detail.Infrastructure,
            ContentHash = contentHash,
            IsActive = true,
            FirstSeenAt = now,
            LastScrapedAt = now
        };
    }

    private static void UpdateEntityFromDetail(SrealListing entity, SrealDetail detail, SrealListItem listItem)
    {
        entity.Title = (detail.Title.Length > 500 ? detail.Title[..500] : detail.Title);
        entity.Address = detail.Address ?? listItem.Address ?? entity.Address;
        entity.City = detail.City ?? listItem.City ?? entity.City;
        entity.PostalCode = detail.PostalCode ?? listItem.PostalCode ?? entity.PostalCode;
        entity.District = detail.District ?? entity.District;
        entity.ObjectType = detail.ObjectType;
        entity.BuyingType = detail.BuyingType ?? entity.BuyingType;
        entity.Price = detail.Price ?? entity.Price;
        entity.PriceText = detail.PriceText ?? entity.PriceText;
        entity.Commission = detail.Commission ?? entity.Commission;
        entity.LivingArea = detail.LivingArea ?? entity.LivingArea;
        entity.PlotArea = detail.PlotArea ?? entity.PlotArea;
        entity.Rooms = detail.Rooms ?? entity.Rooms;
        entity.Description = detail.Description ?? entity.Description;
        entity.EnergyClass = detail.EnergyClass ?? entity.EnergyClass;
        entity.EnergyValue = detail.EnergyValue ?? entity.EnergyValue;
        entity.FGee = detail.FGee ?? entity.FGee;
        entity.FGeeClass = detail.FGeeClass ?? entity.FGeeClass;
        if (detail.ImageUrls.Count > 0)
            entity.ImageUrls = detail.ImageUrls;
        entity.SourceUrl = detail.SourceUrl;
        entity.AgentName = detail.AgentName ?? entity.AgentName;
        entity.AgentPhone = detail.AgentPhone ?? entity.AgentPhone;
        entity.AgentEmail = detail.AgentEmail ?? entity.AgentEmail;
        entity.AgentOffice = detail.AgentOffice ?? entity.AgentOffice;
        entity.Infrastructure = detail.Infrastructure ?? entity.Infrastructure;
    }

    private void LogChange(SrealListing listing, string changeType, string? oldHash, string? newHash, string? changedFields = null)
    {
        var change = new SrealListingChange
        {
            SrealListingId = listing.Id,
            ChangeType = changeType,
            OldContentHash = oldHash,
            NewContentHash = newHash,
            ChangedFields = changedFields
        };

        if (listing.Id == Guid.Empty)
            listing.Changes.Add(change);
        else
            dbContext.Set<SrealListingChange>().Add(change);
    }

    private static string? DetectChangedFields(SrealListing existing, SrealDetail detail)
    {
        var changes = new Dictionary<string, object>();

        void Check(string field, string? oldVal, string? newVal)
        {
            if (oldVal != newVal && newVal != null)
                changes[field] = new { Old = oldVal, New = newVal };
        }

        Check("Title", existing.Title, detail.Title);
        Check("Price", existing.Price?.ToString(), detail.Price?.ToString());
        Check("LivingArea", existing.LivingArea?.ToString(), detail.LivingArea?.ToString());
        Check("PlotArea", existing.PlotArea?.ToString(), detail.PlotArea?.ToString());
        Check("Address", existing.Address, detail.Address);
        Check("AgentName", existing.AgentName, detail.AgentName);

        return changes.Count > 0 ? JsonSerializer.Serialize(changes) : null;
    }

    internal static string ComputeContentHash(Dictionary<string, string> fields)
    {
        var sorted = fields.OrderBy(f => f.Key).Select(f => $"{f.Key}={f.Value}");
        var content = string.Join("|", sorted);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(bytes);
    }
}
