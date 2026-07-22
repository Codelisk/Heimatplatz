using System.Security.Cryptography;
using System.Text;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.WkoCompanies.Configuration;
using Heimatplatz.Api.Features.WkoCompanies.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny;

namespace Heimatplatz.Api.Features.WkoCompanies.Services;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class WkoCompanySyncService(
    IWkoCompanyScraper scraper,
    IFirmenbuchHvdClient firmenbuchClient,
    AppDbContext dbContext,
    IOptions<WkoScrapingOptions> options,
    IOptions<FirmenbuchHvdOptions> firmenbuchOptions,
    ILogger<WkoCompanySyncService> logger
) : IWkoCompanySyncService
{
    // Firmenbuch nennt nur einen Kalendertag, keine Uhrzeit. Auf Wiener MITTAG verankern
    // (nicht Mitternacht) haelt den Kalendertag in jeder Anzeige-Zeitzone stabil - gleiches
    // Muster wie ParsePublicationDate im ForeclosureAuctionSyncService.
    private static readonly TimeZoneInfo ViennaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vienna");

    public async Task<WkoCompanySyncResult> SyncAllAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starte WKO-Firmen-Sync (Region={Region}, {Count} Suchbegriffe)", options.Value.Region, options.Value.BranchKeywords.Count);

        var errors = 0;
        var errorMessages = new List<string>();

        // 1. Alle konfigurierten Suchbegriffe abfragen, ueber die WkoFirmaId deduplizieren.
        // Eine Firma kann unter mehreren Suchbegriffen auftauchen (z.B. "Immobilienmakler" UND
        // "Immobilienverwaltung") - der zuerst gefundene Suchbegriff bleibt als Provenienz stehen.
        var byFirmaId = new Dictionary<Guid, WkoCompanySearchResult>();
        var sourceKeywordByFirmaId = new Dictionary<Guid, string>();

        foreach (var keyword in options.Value.BranchKeywords)
        {
            try
            {
                var keywordResults = await scraper.SearchAsync(keyword, options.Value.Region, ct);
                foreach (var result in keywordResults)
                {
                    if (byFirmaId.TryAdd(result.WkoFirmaId, result))
                        sourceKeywordByFirmaId[result.WkoFirmaId] = keyword;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                errors++;
                var msg = $"Fehler bei Suchbegriff '{keyword}': {ex.Message}";
                errorMessages.Add(msg);
                logger.LogError(ex, "Fehler beim Scrapen des Suchbegriffs '{Keyword}'", keyword);
            }

            await Task.Delay(options.Value.DelayBetweenRequestsMs, ct);
        }

        // Leere Ergebnisse ueber ALLE Suchbegriffe hinweg deuten auf eine Markup-Aenderung hin,
        // nicht auf einen echten Nulltreffer (die konfigurierten Immobilien-Suchbegriffe liefern
        // in Oberoesterreich immer Ergebnisse). Ohne diesen Guard wuerde Schritt 3 unten den
        // GESAMTEN aktiven Bestand als "Removed" deaktivieren.
        if (byFirmaId.Count == 0)
        {
            logger.LogError("WKO-Suche lieferte ueber alle Suchbegriffe hinweg keine Treffer - Sync abgebrochen (Markup-Aenderung der WKO-Seite?)");
            errorMessages.Add("Keine Treffer ueber alle Suchbegriffe - Sync abgebrochen (Markup-Aenderung der WKO-Seite?)");
            return new WkoCompanySyncResult(0, 0, 0, 0, errors + 1, errorMessages);
        }

        var created = 0;
        var updated = 0;
        var removed = 0;
        var unchanged = 0;
        var now = DateTimeOffset.UtcNow;
        var scrapedFirmaIds = new HashSet<Guid>();

        var existingCompanies = await dbContext.Set<WkoCompany>()
            .ToDictionaryAsync(c => c.WkoFirmaId, ct);

        // 2. Fuer jede gefundene Firma die Detailseite laden (Rate-Limiting passiert im Scraper)
        // und upserten.
        foreach (var searchResult in byFirmaId.Values)
        {
            try
            {
                scrapedFirmaIds.Add(searchResult.WkoFirmaId);

                // Inkrementeller Modus: bekannte Firmen NICHT erneut per Detailseite scrapen -
                // nur den "weiterhin gelistet"-Stand nachziehen. Neue Firmen (unbekannte
                // Firmaid) laufen weiter unten durch den vollen Detail-Fetch.
                if (options.Value.SkipDetailsForKnownCompanies
                    && existingCompanies.TryGetValue(searchResult.WkoFirmaId, out var known))
                {
                    known.LastScrapedAt = now;

                    if (!known.IsActive)
                    {
                        known.IsActive = true;
                        known.RemovedAt = null;
                        updated++;
                    }
                    else
                    {
                        unchanged++;
                    }

                    continue;
                }

                var detail = await scraper.GetDetailAsync(searchResult.DetailUrl, ct);
                var contentHash = ComputeContentHash(searchResult, detail);

                if (existingCompanies.TryGetValue(searchResult.WkoFirmaId, out var existing))
                {
                    if (existing.ContentHash == contentHash)
                    {
                        existing.LastScrapedAt = now;

                        if (!existing.IsActive)
                        {
                            existing.IsActive = true;
                            existing.RemovedAt = null;
                            updated++;
                        }
                        else
                        {
                            unchanged++;
                        }
                    }
                    else
                    {
                        ApplyDetailFields(existing, searchResult, detail);
                        existing.ContentHash = contentHash;
                        existing.LastScrapedAt = now;
                        existing.IsActive = true;
                        existing.RemovedAt = null;
                        updated++;
                    }
                }
                else
                {
                    var entity = new WkoCompany
                    {
                        Name = detail.Name ?? searchResult.Name,
                        WkoFirmaId = searchResult.WkoFirmaId,
                        DetailUrl = searchResult.DetailUrl,
                        SourceSearchTerm = sourceKeywordByFirmaId.GetValueOrDefault(searchResult.WkoFirmaId),
                        ContentHash = contentHash,
                        IsActive = true,
                        FirstSeenAt = now,
                        LastScrapedAt = now
                    };
                    ApplyDetailFields(entity, searchResult, detail);

                    dbContext.Set<WkoCompany>().Add(entity);
                    existingCompanies[searchResult.WkoFirmaId] = entity;
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
                var msg = $"Fehler bei Firma {searchResult.WkoFirmaId} ({searchResult.Name}): {ex.Message}";
                errorMessages.Add(msg);
                logger.LogWarning(ex, "Fehler beim Verarbeiten von Firma {FirmaId} ({Name})", searchResult.WkoFirmaId, searchResult.Name);
            }
        }

        // 3. Firmen, die in diesem Lauf nicht mehr gefunden wurden, als inaktiv markieren
        // (Soft-Delete, kein Hard-Delete - konsistent mit dem Zwangsversteigerungs-Sync).
        foreach (var existing in existingCompanies.Values)
        {
            if (existing.IsActive && !scrapedFirmaIds.Contains(existing.WkoFirmaId))
            {
                existing.IsActive = false;
                existing.RemovedAt = now;
                removed++;
            }
        }

        // 4. Amtliche Firmenbuch-Anreicherung: laeuft unabhaengig davon, ob eine Firma in
        // diesem Lauf neu/aktualisiert/uebersprungen wurde - deckt damit auch Bestandsfirmen ab,
        // die vor Konfiguration eines FirmenbuchHvd:ApiKey angelegt wurden. Ohne konfigurierten
        // Key macht IsConfigured/GetAuszugAsync gar keinen HTTP-Request (siehe FirmenbuchHvdClient).
        if (firmenbuchClient.IsConfigured)
        {
            var toEnrich = existingCompanies.Values
                .Where(c => c.FirmenbuchEnrichedAt == null && !string.IsNullOrWhiteSpace(c.CompanyRegisterNumber))
                .ToList();

            logger.LogInformation("Firmenbuch-Anreicherung: {Count} Firmen ohne bisherigen Versuch", toEnrich.Count);

            foreach (var company in toEnrich)
            {
                try
                {
                    var auszug = await firmenbuchClient.GetAuszugAsync(company.CompanyRegisterNumber!, ct);

                    // Nur bei Erfolg als "versucht" markieren - bei Fehlern (Netzwerk, SOAP-Fault,
                    // z.B. wegen eines voruebergehenden Ausfalls) soll der naechste Sync es erneut
                    // probieren statt die Firma dauerhaft unangereichert zu lassen.
                    if (auszug != null)
                    {
                        company.FirmenbuchEnrichedAt = now;
                        company.Euid = auszug.Euid ?? company.Euid;
                        company.FirmenbuchFoundedDate = ToViennaNoonUtc(auszug.FoundedDate) ?? company.FirmenbuchFoundedDate;
                        if (auszug.People.Count > 0)
                            company.FirmenbuchManagingDirectors = auszug.People;
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    errors++;
                    var msg = $"Firmenbuch-Anreicherung fehlgeschlagen fuer {company.Name} ({company.CompanyRegisterNumber}): {ex.Message}";
                    errorMessages.Add(msg);
                    logger.LogWarning(ex, "Firmenbuch-Anreicherung fehlgeschlagen fuer {Name} ({Fnr})", company.Name, company.CompanyRegisterNumber);
                }

                await Task.Delay(firmenbuchOptions.Value.DelayBetweenRequestsMs, ct);
            }
        }

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "WKO-Sync abgeschlossen: {Created} neu, {Updated} aktualisiert, {Removed} entfernt, {Unchanged} unveraendert, {Errors} Fehler",
            created, updated, removed, unchanged, errors);

        return new WkoCompanySyncResult(created, updated, removed, unchanged, errors, errorMessages);
    }

    private static DateTimeOffset? ToViennaNoonUtc(DateOnly? date)
    {
        if (!date.HasValue) return null;

        var viennaNoon = date.Value.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)));
        var utc = TimeZoneInfo.ConvertTimeToUtc(viennaNoon, ViennaTimeZone);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }

    private static void ApplyDetailFields(WkoCompany entity, WkoCompanySearchResult searchResult, WkoCompanyDetail detail)
    {
        entity.Name = detail.Name ?? searchResult.Name;
        entity.DetailUrl = searchResult.DetailUrl;
        entity.CategoryText = detail.CategoryText ?? searchResult.CategoryText;
        entity.Street = detail.Street ?? searchResult.Street;
        entity.PostalCode = detail.PostalCode ?? searchResult.PostalCode;
        entity.City = detail.City ?? searchResult.City;
        entity.Phones = detail.Phones.Count > 0 ? detail.Phones : searchResult.Phones;
        entity.Email = detail.Email ?? searchResult.Email;
        entity.Website = detail.Website ?? searchResult.Website;
        entity.OpeningHoursText = detail.OpeningHoursText;
        entity.CompanyRegisterNumber = detail.CompanyRegisterNumber;
        entity.CompanyCourt = detail.CompanyCourt;
        entity.Gln = detail.Gln;
        entity.LegalForm = detail.LegalForm;
        entity.FoundedYear = detail.FoundedYear;
        // Fruehestes "Seit"-Datum der Berechtigungen als Gruendungsdatum-Naeherung
        // (Min ueberspringt null-Werte und liefert null, wenn kein Datum vorhanden ist)
        entity.FoundedDate = detail.Permits.Min(p => p.Since) ?? entity.FoundedDate;
        entity.IsTrainingCompany = searchResult.IsTrainingCompany;
        entity.Permits = detail.Permits;
    }

    private static string ComputeContentHash(WkoCompanySearchResult searchResult, WkoCompanyDetail detail)
    {
        var phones = detail.Phones.Count > 0 ? detail.Phones : searchResult.Phones;
        var fields = new SortedDictionary<string, string>
        {
            ["Name"] = detail.Name ?? searchResult.Name,
            ["CategoryText"] = detail.CategoryText ?? searchResult.CategoryText ?? "",
            ["Street"] = detail.Street ?? searchResult.Street ?? "",
            ["PostalCode"] = detail.PostalCode ?? searchResult.PostalCode ?? "",
            ["City"] = detail.City ?? searchResult.City ?? "",
            ["Phones"] = string.Join(",", phones),
            ["Email"] = detail.Email ?? searchResult.Email ?? "",
            ["Website"] = detail.Website ?? searchResult.Website ?? "",
            ["OpeningHoursText"] = detail.OpeningHoursText ?? "",
            ["CompanyRegisterNumber"] = detail.CompanyRegisterNumber ?? "",
            ["CompanyCourt"] = detail.CompanyCourt ?? "",
            ["Gln"] = detail.Gln ?? "",
            ["LegalForm"] = detail.LegalForm ?? "",
            ["FoundedYear"] = detail.FoundedYear?.ToString() ?? "",
            ["IsTrainingCompany"] = searchResult.IsTrainingCompany.ToString(),
            ["Permits"] = string.Join("|", detail.Permits.Select(p => $"{p.FachgruppeName}:{p.Description}:{p.ManagingDirector}:{p.GisaNumber}:{p.Since:yyyy-MM-dd}"))
        };

        var content = string.Join("|", fields.Select(f => $"{f.Key}={f.Value}"));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(bytes);
    }
}
