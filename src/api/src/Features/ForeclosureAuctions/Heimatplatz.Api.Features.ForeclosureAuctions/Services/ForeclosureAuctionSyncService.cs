using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.ForeclosureAuctions.Configuration;
using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts;
using Heimatplatz.Api.Features.ForeclosureAuctions.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Services;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public partial class ForeclosureAuctionSyncService(
    IEdikteScraper scraper,
    AppDbContext dbContext,
    IOptions<ScrapingOptions> scrapingOptions,
    IForeclosurePropertySyncService propertySyncService,
    ILogger<ForeclosureAuctionSyncService> logger
) : IForeclosureAuctionSyncService
{
    public async Task<SyncResult> SyncAllAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starte Zwangsversteigerungs-Sync");

        var created = 0;
        var updated = 0;
        var removed = 0;
        var unchanged = 0;
        var errors = 0;
        var errorMessages = new List<string>();

        // 1. Alle Edikte von der Website holen
        List<EdiktListItem> listItems;
        try
        {
            listItems = await scraper.GetAuctionListAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Abrufen der Edikte-Liste");
            return new SyncResult(0, 0, 0, 0, 1, [$"Fehler beim Abrufen der Liste: {ex.Message}"]);
        }

        // 1b. Ausgeschlossene Kategorien filtern (z.B. keine Wohnungen)
        var excludedCategories = scrapingOptions.Value.ExcludedCategories;
        if (excludedCategories.Count > 0)
        {
            var before = listItems.Count;
            listItems = listItems
                .Where(item => !excludedCategories.Any(exc =>
                    item.CategoryText?.Contains(exc, StringComparison.OrdinalIgnoreCase) == true))
                .ToList();
            logger.LogInformation(
                "{Filtered} von {Total} Eintraegen nach Kategorie-Filter uebersprungen",
                before - listItems.Count, before);
        }

        var now = DateTimeOffset.UtcNow;
        var scrapedExternalIds = new HashSet<string>();

        // 2. Bestehende Eintraege laden
        Dictionary<string, ForeclosureAuction> existingAuctions;
        try
        {
            existingAuctions = await dbContext.Set<ForeclosureAuction>()
                .Where(a => a.ExternalId != null)
                .ToDictionaryAsync(a => a.ExternalId!, ct);
        }
        catch (Exception ex) when (ex.Message.Contains("Invalid column") || ex.InnerException?.Message?.Contains("Invalid column") == true
                                   || ex.Message.Contains("no such column") || ex.InnerException?.Message?.Contains("no such column") == true)
        {
            logger.LogWarning(ex, "Schema mismatch detected - recreating database to apply new schema");
            await dbContext.Database.EnsureDeletedAsync(ct);
            await dbContext.Database.EnsureCreatedAsync(ct);
            existingAuctions = new Dictionary<string, ForeclosureAuction>();
        }

        // 3. Fuer jeden Listeneintrag die Details holen und verarbeiten
        foreach (var item in listItems)
        {
            try
            {
                scrapedExternalIds.Add(item.ExternalId);

                var detail = await scraper.GetAuctionDetailAsync(item.ExternalId, ct);
                var contentHash = ComputeContentHash(detail.AllFields);

                if (existingAuctions.TryGetValue(item.ExternalId, out var existing))
                {
                    // Existiert bereits - pruefen ob sich etwas geaendert hat
                    if (existing.ContentHash == contentHash)
                    {
                        // Keine Aenderung - nur LastScrapedAt aktualisieren
                        existing.LastScrapedAt = now;

                        // Reappeared?
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
                        // Geaendert - Entity aktualisieren
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
                    // Neuer Eintrag
                    var auction = CreateEntityFromDetail(detail, item, contentHash, now);
                    dbContext.Set<ForeclosureAuction>().Add(auction);
                    LogChange(auction, "Created", null, contentHash);
                    created++;
                }
            }
            catch (Exception ex)
            {
                errors++;
                var msg = $"Fehler bei Edikt {item.ExternalId}: {ex.Message}";
                errorMessages.Add(msg);
                logger.LogWarning(ex, "Fehler beim Verarbeiten von Edikt {ExternalId}", item.ExternalId);
            }
        }

        // 4. Entfernte Eintraege markieren (in DB aber nicht mehr auf Website)
        foreach (var existing in existingAuctions.Values)
        {
            if (existing.IsActive && !scrapedExternalIds.Contains(existing.ExternalId!))
            {
                existing.IsActive = false;
                existing.RemovedAt = now;
                LogChange(existing, "Removed", existing.ContentHash, null);
                removed++;
            }
        }

        await dbContext.SaveChangesAsync(ct);

        // 5. Properties aus Zwangsversteigerungen synchronisieren
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
            logger.LogError(ex, "Fehler beim Property-Sync nach Zwangsversteigerungs-Sync");
            errorMessages.Add($"Property-Sync fehlgeschlagen: {ex.Message}");
            errors++;
        }

        logger.LogInformation(
            "Sync abgeschlossen: {Created} neu, {Updated} aktualisiert, {Removed} entfernt, {Unchanged} unveraendert, {Errors} Fehler",
            created, updated, removed, unchanged, errors);

        return new SyncResult(created, updated, removed, unchanged, errors, errorMessages);
    }

    private ForeclosureAuction CreateEntityFromDetail(EdiktDetail detail, EdiktListItem listItem, string contentHash, DateTimeOffset now)
    {
        var auction = new ForeclosureAuction
        {
            AuctionDate = ParseAuctionDate(detail.AuctionDateText) ?? DateTimeOffset.MinValue,
            Category = ParseCategory(detail.CategoryText),
            ObjectDescription = detail.ObjectDescription ?? listItem.ObjectDescription ?? "Keine Beschreibung",
            Status = detail.StatusText,
            Address = detail.Address ?? listItem.Address ?? "Keine Adresse",
            City = ExtractCity(detail.PostalCodeAndCity) ?? listItem.City ?? "Unbekannt",
            PostalCode = ExtractPostalCode(detail.PostalCodeAndCity) ?? listItem.PostalCode ?? "0000",
            RegistrationNumber = detail.RegistrationNumber,
            CadastralMunicipality = detail.CadastralMunicipality,
            PlotNumber = detail.PlotNumber,
            SheetNumber = detail.SheetNumber,
            TotalArea = ParseArea(detail.PlotAreaText),
            PlotArea = ParseArea(detail.PlotAreaText),
            BuildingArea = ParseArea(detail.ObjectAreaText),
            EstimatedValue = ParseCurrency(detail.EstimatedValueText),
            MinimumBid = ParseCurrency(detail.MinimumBidText),
            CaseNumber = detail.CaseNumber,
            Court = detail.Court,
            EdictUrl = $"{scraper.GetType().Name}/{detail.ExternalId}",
            ShortAppraisalUrl = detail.ShortAppraisalUrl,
            LongAppraisalUrl = detail.LongAppraisalUrl,
            SitePlanUrl = detail.SitePlanUrl,
            FloorPlanUrl = detail.FloorPlanUrl,
            ExternalId = detail.ExternalId,
            ContentHash = contentHash,
            State = GuessStateFromPostalCode(ExtractPostalCode(detail.PostalCodeAndCity)),
            IsActive = true,
            FirstSeenAt = now,
            LastScrapedAt = now
        };

        // EdictUrl korrekt setzen
        auction.EdictUrl = $"https://edikte.justiz.gv.at/edikte/ex/exedi3.nsf/alldoc/{detail.ExternalId}!OpenDocument";

        return auction;
    }

    private void UpdateEntityFromDetail(ForeclosureAuction entity, EdiktDetail detail, EdiktListItem listItem)
    {
        entity.AuctionDate = ParseAuctionDate(detail.AuctionDateText) ?? entity.AuctionDate;
        entity.Category = ParseCategory(detail.CategoryText);
        entity.ObjectDescription = detail.ObjectDescription ?? listItem.ObjectDescription ?? entity.ObjectDescription;
        entity.Status = detail.StatusText;
        entity.Address = detail.Address ?? listItem.Address ?? entity.Address;
        entity.City = ExtractCity(detail.PostalCodeAndCity) ?? listItem.City ?? entity.City;
        entity.PostalCode = ExtractPostalCode(detail.PostalCodeAndCity) ?? listItem.PostalCode ?? entity.PostalCode;
        entity.RegistrationNumber = detail.RegistrationNumber ?? entity.RegistrationNumber;
        entity.CadastralMunicipality = detail.CadastralMunicipality ?? entity.CadastralMunicipality;
        entity.PlotNumber = detail.PlotNumber ?? entity.PlotNumber;
        entity.SheetNumber = detail.SheetNumber ?? entity.SheetNumber;
        entity.TotalArea = ParseArea(detail.PlotAreaText) ?? entity.TotalArea;
        entity.PlotArea = ParseArea(detail.PlotAreaText) ?? entity.PlotArea;
        entity.BuildingArea = ParseArea(detail.ObjectAreaText) ?? entity.BuildingArea;
        entity.EstimatedValue = ParseCurrency(detail.EstimatedValueText) ?? entity.EstimatedValue;
        entity.MinimumBid = ParseCurrency(detail.MinimumBidText) ?? entity.MinimumBid;
        entity.CaseNumber = detail.CaseNumber ?? entity.CaseNumber;
        entity.Court = detail.Court ?? entity.Court;
        entity.ShortAppraisalUrl = detail.ShortAppraisalUrl ?? entity.ShortAppraisalUrl;
        entity.LongAppraisalUrl = detail.LongAppraisalUrl ?? entity.LongAppraisalUrl;
        entity.SitePlanUrl = detail.SitePlanUrl ?? entity.SitePlanUrl;
        entity.FloorPlanUrl = detail.FloorPlanUrl ?? entity.FloorPlanUrl;
        entity.State = GuessStateFromPostalCode(ExtractPostalCode(detail.PostalCodeAndCity)) ?? entity.State;
        entity.EdictUrl = $"https://edikte.justiz.gv.at/edikte/ex/exedi3.nsf/alldoc/{detail.ExternalId}!OpenDocument";
    }

    private void LogChange(ForeclosureAuction auction, string changeType, string? oldHash, string? newHash, string? changedFields = null)
    {
        var change = new ForeclosureAuctionChange
        {
            ForeclosureAuctionId = auction.Id,
            ChangeType = changeType,
            OldContentHash = oldHash,
            NewContentHash = newHash,
            ChangedFields = changedFields
        };

        // Fuer neue Entities die noch keine ID haben
        if (auction.Id == Guid.Empty)
            auction.Changes.Add(change);
        else
            dbContext.Set<ForeclosureAuctionChange>().Add(change);
    }

    private static string? DetectChangedFields(ForeclosureAuction existing, EdiktDetail detail)
    {
        var changes = new Dictionary<string, object>();

        void Check(string field, string? oldVal, string? newVal)
        {
            if (oldVal != newVal && newVal != null)
                changes[field] = new { Old = oldVal, New = newVal };
        }

        Check("Status", existing.Status, detail.StatusText);
        Check("Court", existing.Court, detail.Court);
        Check("CaseNumber", existing.CaseNumber, detail.CaseNumber);
        Check("EstimatedValue", existing.EstimatedValue?.ToString(CultureInfo.InvariantCulture), detail.EstimatedValueText);
        Check("MinimumBid", existing.MinimumBid?.ToString(CultureInfo.InvariantCulture), detail.MinimumBidText);
        Check("Address", existing.Address, detail.Address);

        return changes.Count > 0 ? JsonSerializer.Serialize(changes) : null;
    }

    // === Parsing-Hilfsmethoden ===

    private static string ComputeContentHash(Dictionary<string, string> fields)
    {
        var sorted = fields.OrderBy(f => f.Key).Select(f => $"{f.Key}={f.Value}");
        var content = string.Join("|", sorted);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(bytes);
    }

    private static DateTimeOffset? ParseAuctionDate(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        // Pattern: "am 27.2.2026 um 11:00 Uhr" oder "am 20.05.2026 um 10:00 Uhr"
        var match = DatePattern().Match(text);
        if (!match.Success) return null;

        var dateStr = match.Groups[1].Value;
        var timeStr = match.Groups[2].Value;

        if (DateTimeOffset.TryParseExact(
            $"{dateStr} {timeStr}",
            ["d.M.yyyy HH:mm", "dd.MM.yyyy HH:mm", "d.MM.yyyy HH:mm", "dd.M.yyyy HH:mm"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var result))
        {
            // CET/CEST: Wien ist UTC+1 oder UTC+2
            return result.ToOffset(TimeSpan.FromHours(1));
        }

        return null;
    }

    private static PropertyCategory ParseCategory(string? text)
    {
        if (string.IsNullOrEmpty(text)) return PropertyCategory.Sonstiges;

        return text.ToLowerInvariant() switch
        {
            "einfamilienhaus" => PropertyCategory.Einfamilienhaus,
            "zweifamilienhaus" => PropertyCategory.Zweifamilienhaus,
            var t when t.Contains("mehrfamilien") || t.Contains("mietwohn") || t.Contains("mietshaus")
                || t.Contains("reihenhaus") || t.Contains("gemischt") || t.Contains("hausanteil")
                => PropertyCategory.Mehrfamilienhaus,
            var t when t.Contains("wohnungseigentum") || t.Contains("eigentumswohn")
                || t.Contains("maisonette") || t.Contains("dachterrassen") || t.Contains("dachgeschoß")
                || t.Contains("garconniere") || t.Contains("gartenwohn")
                => PropertyCategory.Wohnungseigentum,
            var t when t.Contains("gewerblich") => PropertyCategory.GewerblicheLiegenschaft,
            var t when t.Contains("unbebaut") || t.Contains("bebaubar") => PropertyCategory.Grundstueck,
            var t when t.Contains("land- und forstwirtschaft") => PropertyCategory.LandUndForstwirtschaft,
            var t when t.Contains("superädifikat") || t.Contains("baurecht") => PropertyCategory.Sonstiges,
            _ => PropertyCategory.Sonstiges
        };
    }

    private static decimal? ParseCurrency(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        // Pattern: "577.600,00 EUR" oder "151.000,00 EUR"
        var cleaned = text.Replace("EUR", "").Replace("€", "").Trim();
        // Oesterreichisches Format: Punkt als Tausendertrenner, Komma als Dezimaltrenner
        cleaned = cleaned.Replace(".", "").Replace(",", ".");

        return decimal.TryParse(cleaned, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    private static decimal? ParseArea(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        // Pattern: "576 m²" oder "1.556 m²" oder "67,93 m²"
        var cleaned = text.Replace("m²", "").Replace("m2", "").Trim();
        cleaned = cleaned.Replace(".", "").Replace(",", ".");

        return decimal.TryParse(cleaned, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    private static string? ExtractPostalCode(string? plzOrt)
    {
        if (string.IsNullOrEmpty(plzOrt)) return null;
        var match = PlzExtractPattern().Match(plzOrt);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractCity(string? plzOrt)
    {
        if (string.IsNullOrEmpty(plzOrt)) return null;
        var match = PlzExtractPattern().Match(plzOrt);
        return match.Success ? plzOrt[match.Length..].Trim() : plzOrt.Trim();
    }

    private static AustrianState? GuessStateFromPostalCode(string? plz)
    {
        if (string.IsNullOrEmpty(plz) || plz.Length < 1) return null;

        // Oesterreichische PLZ-Bereiche
        return plz[0] switch
        {
            '1' => AustrianState.Wien,
            '2' => plz.StartsWith("23") || plz.StartsWith("24") || plz.StartsWith("25")
                    || plz.StartsWith("26") || plz.StartsWith("27") || plz.StartsWith("28")
                ? AustrianState.Niederoesterreich
                : AustrianState.Niederoesterreich,
            '3' => AustrianState.Niederoesterreich,
            '4' => AustrianState.Oberoesterreich,
            '5' => AustrianState.Salzburg,
            '6' => plz.StartsWith("69") ? AustrianState.Vorarlberg : AustrianState.Tirol,
            '7' => AustrianState.Burgenland,
            '8' => AustrianState.Steiermark,
            '9' => plz.StartsWith("94") || plz.StartsWith("95") || plz.StartsWith("96") || plz.StartsWith("97") || plz.StartsWith("98")
                ? AustrianState.Kaernten
                : AustrianState.Kaernten,
            _ => null
        };
    }

    [GeneratedRegex(@"am\s+(\d{1,2}\.\d{1,2}\.\d{4})\s+um\s+(\d{2}:\d{2})")]
    private static partial Regex DatePattern();

    [GeneratedRegex(@"^(\d{4})\s")]
    private static partial Regex PlzExtractPattern();
}
