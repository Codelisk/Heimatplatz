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
using Shiny;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Services;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public partial class ForeclosureAuctionSyncService(
    IEdikteScraper scraper,
    IForeclosureImageService imageService,
    AppDbContext dbContext,
    IOptions<ScrapingOptions> scrapingOptions,
    IForeclosurePropertySyncService propertySyncService,
    ILogger<ForeclosureAuctionSyncService> logger
) : IForeclosureAuctionSyncService
{
    // Ediktsdatei liefert Wiener Ortszeit (CET/CEST) - "Europe/Vienna" ist die
    // plattformuebergreifend gueltige IANA-Zone (funktioniert auf Linux/macOS/Windows).
    private static readonly TimeZoneInfo ViennaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vienna");
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

        // Eine leere Liste ist mit dem konfigurierten Suchfilter praktisch ausgeschlossen -
        // wahrscheinlicher hat sich das Seiten-Markup geaendert und der Parser findet die
        // Tabelle nicht mehr. Ohne diesen Guard wuerde Schritt 4 unten den GESAMTEN
        // aktiven Bestand als "Removed" deaktivieren.
        if (listItems.Count == 0)
        {
            logger.LogError("Edikte-Ergebnisliste ist leer - Sync abgebrochen (Markup-Aenderung der Ediktsdatei?)");
            return new SyncResult(0, 0, 0, 0, 1, ["Edikte-Ergebnisliste ist leer - Sync abgebrochen (Markup-Aenderung der Ediktsdatei?)"]);
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
        var existingAuctions = await dbContext.Set<ForeclosureAuction>()
            .Where(a => a.ExternalId != null)
            .ToDictionaryAsync(a => a.ExternalId!, ct);

        // 2b. Case-Dedup im Bestand: Die Ediktsdatei publiziert zur selben Sache mitunter
        // MEHRERE Dokumente mit eigener Dokument-ID (Original-Edikt + geaendertes Edikt).
        // Da der Sync auf ExternalId (= Dokument) keyt, wurde dieselbe Liegenschaft dann
        // doppelt als aktive Auktion (und damit doppelt als Inserat) gefuehrt. Gleiches
        // Verfahren + gleiches Grundbuchsobjekt = gleiche Sache: nur die aktuellste
        // Publikation bleibt aktiv, aeltere Dokumente werden deaktiviert.
        var duplicatesDeactivated = 0;
        var activeByCaseKey = new Dictionary<string, ForeclosureAuction>();
        foreach (var caseGroup in existingAuctions.Values
                     .Where(a => a.IsActive)
                     .Select(a => (Auction: a, Key: BuildCaseKey(a.Court, a.CaseNumber, a.CadastralMunicipality, a.RegistrationNumber, a.PlotNumber)))
                     .Where(x => x.Key != null)
                     .GroupBy(x => x.Key!))
        {
            var winner = caseGroup
                .Select(x => x.Auction)
                .OrderByDescending(a => a.PublicationDate ?? DateTimeOffset.MinValue)
                .ThenByDescending(a => a.FirstSeenAt ?? DateTimeOffset.MinValue)
                .ThenByDescending(a => a.ExternalId, StringComparer.Ordinal)
                .First();
            activeByCaseKey[caseGroup.Key!] = winner;

            foreach (var loser in caseGroup.Select(x => x.Auction).Where(a => !ReferenceEquals(a, winner)))
            {
                loser.IsActive = false;
                loser.RemovedAt = now;
                loser.LastScrapedAt = now;
                LogChange(loser, "DuplicateCase", loser.ContentHash, null);
                duplicatesDeactivated++;
            }
        }

        var skippedConcluded = 0;
        var skippedExcluded = 0;

        // 3. Fuer jeden Listeneintrag die Details holen und verarbeiten
        foreach (var item in listItems)
        {
            try
            {
                scrapedExternalIds.Add(item.ExternalId);

                var detail = await scraper.GetAuctionDetailAsync(item.ExternalId, ct);

                // Abgeschlossene Verfahren ("Zuschlag mit/ohne Ueberbot", "Meistbotsverteilung",
                // "Verschiebung" ohne neuen Termin, ...) haben keinen gueltigen zukuenftigen
                // Versteigerungstermin - die Liegenschaft ist nicht (mehr) ersteigerbar und darf
                // nicht als aktives Inserat gefuehrt werden.
                // Das Datum allein reicht aber nicht: "Entfall des Termins"-Edikte fuehren den
                // ENTFALLENEN Termin weiterhin unter "Termin:" - liegt der in der Zukunft, saehe
                // die Auktion faelschlich aktiv aus. Deshalb zusaetzlich den Edikt-Typ pruefen.
                var auctionDate = ParseAuctionDate(detail.AuctionDateText);
                var isUpcoming = auctionDate.HasValue
                    && auctionDate.Value > now
                    && !IsConcludedEdictType(detail.Title);

                // Kategorie-Ausschluss zusaetzlich gegen den zuverlaessigeren Detailseiten-Text
                // pruefen - die Listenseiten-Kategorie (GetAuctionListAsync) ist eine grobe
                // Heuristik und kann z.B. Wohnungen trotz Konfiguration durchlassen.
                var isExcludedCategory = excludedCategories.Count > 0
                    && excludedCategories.Any(exc =>
                        detail.CategoryText?.Contains(exc, StringComparison.OrdinalIgnoreCase) == true);

                if (!isUpcoming || isExcludedCategory)
                {
                    if (existingAuctions.TryGetValue(item.ExternalId, out var concluded) && concluded.IsActive)
                    {
                        concluded.IsActive = false;
                        concluded.RemovedAt = now;
                        concluded.LastScrapedAt = now;
                        LogChange(concluded, "Concluded", concluded.ContentHash, null);
                        removed++;

                        // Sache aus dem Aktiv-Index nehmen, damit ein spaeteres Edikt zur
                        // selben Sache wieder regulaer als neue Auktion angelegt werden kann.
                        var concludedKey = BuildCaseKey(concluded.Court, concluded.CaseNumber, concluded.CadastralMunicipality, concluded.RegistrationNumber, concluded.PlotNumber);
                        if (concludedKey != null
                            && activeByCaseKey.TryGetValue(concludedKey, out var mapped)
                            && ReferenceEquals(mapped, concluded))
                        {
                            activeByCaseKey.Remove(concludedKey);
                        }
                    }
                    else if (!isUpcoming)
                    {
                        skippedConcluded++;
                    }
                    else
                    {
                        skippedExcluded++;
                    }

                    continue;
                }

                // isUpcoming garantiert HasValue - Compiler kann das ueber den fruehen
                // continue-Ausstieg oben nicht selbst herleiten.
                var confirmedAuctionDate = auctionDate!.Value;

                // Case-Dedup: gehoert dieses Edikt-Dokument zu einer Sache, die bereits
                // ueber ein ANDERES aktives Dokument gefuehrt wird? Dann kein zweiter
                // Datensatz: neuere Publikation uebernimmt die bestehende Auktion
                // (Supersede), aeltere/gleichzeitige Zweitpublikationen werden ignoriert.
                var caseKey = BuildCaseKey(detail.Court, detail.CaseNumber, detail.CadastralMunicipality, detail.RegistrationNumber, detail.PlotNumber);
                if (caseKey != null
                    && activeByCaseKey.TryGetValue(caseKey, out var caseWinner)
                    && !(existingAuctions.TryGetValue(item.ExternalId, out var caseSelf) && ReferenceEquals(caseSelf, caseWinner)))
                {
                    var newPublication = ParsePublicationDate(detail.PublicationDateText);
                    var supersedes = !existingAuctions.ContainsKey(item.ExternalId)
                        && newPublication.HasValue
                        && (!caseWinner.PublicationDate.HasValue || newPublication.Value > caseWinner.PublicationDate.Value);

                    if (supersedes)
                    {
                        var supersedeImageUrls = await imageService.PrepareImageUrlsAsync(detail, ct);
                        detail = detail with { ImageUrls = supersedeImageUrls };
                        var newHash = ComputeContentHash(detail.AllFields, detail.ImageUrls);
                        var oldHash = caseWinner.ContentHash;
                        var oldExternalId = caseWinner.ExternalId;
                        UpdateEntityFromDetail(caseWinner, detail, item, confirmedAuctionDate);
                        caseWinner.ExternalId = detail.ExternalId;
                        caseWinner.ContentHash = newHash;
                        caseWinner.LastScrapedAt = now;
                        caseWinner.IsActive = true;
                        caseWinner.RemovedAt = null;
                        LogChange(caseWinner, "Superseded", oldHash, newHash);
                        if (oldExternalId != null)
                            existingAuctions.Remove(oldExternalId);
                        existingAuctions[detail.ExternalId] = caseWinner;
                        updated++;
                    }
                    else
                    {
                        if (caseSelf != null)
                        {
                            caseSelf.LastScrapedAt = now;
                            if (caseSelf.IsActive)
                            {
                                caseSelf.IsActive = false;
                                caseSelf.RemovedAt = now;
                                LogChange(caseSelf, "DuplicateCase", caseSelf.ContentHash, null);
                                duplicatesDeactivated++;
                            }
                        }

                        unchanged++;
                    }

                    continue;
                }

                // Nur fuer aktive, nicht ausgeschlossene Edikte Bilder bewerten.
                // Die PDF-Extraktion ist ein gecachter Fallback und laeuft nur,
                // wenn die direkten Justiz-Anhaenge technisch unbrauchbar sind.
                var preparedImageUrls = await imageService.PrepareImageUrlsAsync(detail, ct);
                detail = detail with { ImageUrls = preparedImageUrls };
                var contentHash = ComputeContentHash(detail.AllFields, detail.ImageUrls);

                if (existingAuctions.TryGetValue(item.ExternalId, out var existing))
                {
                    // Aktiv-Index nachziehen: Court/CaseNumber koennen im Detail erstmals
                    // auftauchen, dann war die Sache bisher nicht indiziert.
                    if (caseKey != null)
                        activeByCaseKey[caseKey] = existing;

                    // Existiert bereits - pruefen ob sich etwas geaendert hat
                    if (existing.ContentHash == contentHash)
                    {
                        // Keine Aenderung - nur LastScrapedAt aktualisieren
                        existing.LastScrapedAt = now;

                        // Publikationsdatum bei jedem Lauf neu parsen statt nur bei null: Edikte aus
                        // der Zeit vor dem Parser-Fix haben entweder gar kein PublicationDate oder
                        // eines mit dem alten Mitternachts-Anker (= Vortag in jeder UTC-Anzeige).
                        // Der ContentHash aendert sich dadurch nicht (das Feld steckte immer in
                        // AllFields), deshalb wuerde dieser Zweig beides sonst bis zur naechsten
                        // inhaltlichen Aenderung nie korrigieren. Das Parsen ist deterministisch,
                        // erneutes Anwenden also unschaedlich - wie im Changed-Zweig unten.
                        existing.PublicationDate = ParsePublicationDate(detail.PublicationDateText)
                            ?? existing.PublicationDate;

                        // Gleiches Backfill-Muster fuer Felder, deren ABLEITUNG sich geaendert hat,
                        // ohne dass der ContentHash es bemerkt: Status stammt inzwischen aus dem
                        // Seitentitel statt dem aeltesten Protokolleintrag ("Sonstiges Edikt"),
                        // State aus dem konfigurierten Bundesland-Filter statt der PLZ-Heuristik.
                        existing.Status = detail.StatusText ?? existing.Status;
                        existing.State = ResolveState(detail) ?? existing.State;

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
                        // Geaendert - Entity aktualisieren. Alten Hash VOR dem Ueberschreiben
                        // sichern, sonst protokolliert der Changelog Old == New.
                        var changedFields = DetectChangedFields(existing, detail);
                        var oldContentHash = existing.ContentHash;
                        UpdateEntityFromDetail(existing, detail, item, confirmedAuctionDate);
                        existing.ContentHash = contentHash;
                        existing.LastScrapedAt = now;
                        existing.IsActive = true;
                        existing.RemovedAt = null;
                        LogChange(existing, "Updated", oldContentHash, contentHash, changedFields);
                        updated++;
                    }
                }
                else
                {
                    // Neuer Eintrag
                    var auction = CreateEntityFromDetail(detail, item, contentHash, confirmedAuctionDate, now);
                    dbContext.Set<ForeclosureAuction>().Add(auction);
                    LogChange(auction, "Created", null, contentHash);
                    if (caseKey != null)
                        activeByCaseKey[caseKey] = auction;
                    created++;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Abbruch sauber durchreichen - sonst wuerde jedes restliche Edikt mit dem
                // bereits gecancelten Token einzeln als Fehler durchlaufen.
                throw;
            }
            catch (Exception ex)
            {
                errors++;
                var msg = $"Fehler bei Edikt {item.ExternalId}: {ex.Message}";
                errorMessages.Add(msg);
                logger.LogWarning(ex, "Fehler beim Verarbeiten von Edikt {ExternalId}", item.ExternalId);
            }
        }

        if (skippedConcluded > 0 || skippedExcluded > 0)
        {
            logger.LogInformation(
                "{Concluded} Edikte ohne gueltigen Termin (abgeschlossen/verschoben) und {Excluded} nach Kategorie-Ausschluss uebersprungen",
                skippedConcluded, skippedExcluded);
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

        // Kein Auto-Heal per EnsureDeleted/EnsureCreated mehr (siehe Git-Historie): das haette
        // bei einem Schema-/Truncation-Fehler die GESAMTE Produktionsdatenbank geloescht und
        // leer neu angelegt - nicht nur diese Tabelle. Ein echtes Schema-Problem gehoert per
        // EF-Migration behoben, nicht durch Datenverlust "repariert". Der Fehler wird hier
        // bewusst durchgereicht; der aufrufende Handler faengt und loggt ihn.
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
            errorMessages.Add($"Property-Sync fehlgeschlagen: {ex.Message} | Inner: {ex.InnerException?.Message}");
            errors++;
        }

        logger.LogInformation(
            "Sync abgeschlossen: {Created} neu, {Updated} aktualisiert, {Removed} entfernt, {Duplicates} Case-Duplikate deaktiviert, {Unchanged} unveraendert, {Errors} Fehler",
            created, updated, removed, duplicatesDeactivated, unchanged, errors);

        return new SyncResult(created, updated, removed + duplicatesDeactivated, unchanged, errors, errorMessages);
    }

    /// <summary>
    /// Fachlicher Schluessel einer Versteigerungssache: Gericht + Aktenzeichen + Grundbuchsobjekt
    /// (KG/EZ/GST). Mehrere Edikt-Dokumente derselben Sache (Original + geaendertes Edikt) teilen
    /// ihn; verschiedene Liegenschaften EINES Verfahrens unterscheiden sich im Grundbuchsteil.
    /// Ohne Gericht oder Aktenzeichen gibt es keinen verlaesslichen Schluessel (null = kein Dedup).
    /// </summary>
    internal static string? BuildCaseKey(string? court, string? caseNumber, string? cadastralMunicipality, string? registrationNumber, string? plotNumber)
    {
        if (string.IsNullOrWhiteSpace(court) || string.IsNullOrWhiteSpace(caseNumber))
            return null;

        static string Norm(string? value) =>
            string.Join(' ', (value ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

        return string.Join('|', Norm(court), Norm(caseNumber), Norm(cadastralMunicipality), Norm(registrationNumber), Norm(plotNumber));
    }

    private ForeclosureAuction CreateEntityFromDetail(EdiktDetail detail, EdiktListItem listItem, string contentHash, DateTimeOffset auctionDate, DateTimeOffset now)
    {
        var auction = new ForeclosureAuction
        {
            // Aufrufer garantiert bereits einen gueltigen, in der Zukunft liegenden Termin
            // (siehe isUpcoming-Pruefung in SyncAllAsync) - kein MinValue-Fallback mehr noetig.
            AuctionDate = auctionDate,
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
            EdictUrl = BuildEdictUrl(detail.ExternalId),
            ShortAppraisalUrl = detail.ShortAppraisalUrl,
            LongAppraisalUrl = detail.LongAppraisalUrl,
            SitePlanUrl = detail.SitePlanUrl,
            FloorPlanUrl = detail.FloorPlanUrl,
            ImageUrls = detail.ImageUrls,
            ExternalId = detail.ExternalId,
            ContentHash = contentHash,
            State = ResolveState(detail),
            PublicationDate = ParsePublicationDate(detail.PublicationDateText),
            IsActive = true,
            FirstSeenAt = now,
            LastScrapedAt = now
        };

        return auction;
    }

    private void UpdateEntityFromDetail(ForeclosureAuction entity, EdiktDetail detail, EdiktListItem listItem, DateTimeOffset auctionDate)
    {
        entity.AuctionDate = auctionDate;
        entity.PublicationDate = ParsePublicationDate(detail.PublicationDateText) ?? entity.PublicationDate;
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
        if (detail.ImageUrls.Count > 0)
            entity.ImageUrls = detail.ImageUrls;
        entity.State = ResolveState(detail) ?? entity.State;
        entity.EdictUrl = BuildEdictUrl(detail.ExternalId);
    }

    private string BuildEdictUrl(string externalId) =>
        $"{scrapingOptions.Value.BaseUrl.TrimEnd('/')}/edikte/ex/exedi3.nsf/alldoc/{externalId}!OpenDocument";

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

    private static string ComputeContentHash(Dictionary<string, string> fields, List<string> imageUrls)
    {
        // Bilder gehoeren zum Inhalt: Aenderungen an den Attachments (oder an der
        // Extraktion, z.B. Dedupe/Sortierung) loesen so ein Update des Altbestands aus
        var sorted = fields.OrderBy(f => f.Key).Select(f => $"{f.Key}={f.Value}");
        var content = string.Join("|", sorted.Concat(imageUrls));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(bytes);
    }

    internal static DateTimeOffset? ParseAuctionDate(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        // Pattern: "am 27.2.2026 um 11:00 Uhr" oder "am 20.05.2026 um 10:00 Uhr".
        // Die Ediktsdatei liefert auch EINSTELLIGE Stunden ("am 17.8.2026 um 9:00 Uhr") -
        // frueher verlangte das Pattern \d{2}:\d{2}, wodurch solche Versteigerungen als
        // "abgeschlossen" uebersprungen wurden und im Bestand fehlten.
        var match = DatePattern().Match(text);
        if (!match.Success) return null;

        var dateStr = match.Groups[1].Value;
        var timeStr = match.Groups[2].Value;

        if (DateTime.TryParseExact(
            $"{dateStr} {timeStr}",
            "d.M.yyyy H:mm", // d/M/H matchen beim Parsen jeweils ein- UND zweistellig
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var localDateTime))
        {
            // Wiener Ortszeit korrekt (inkl. CET/CEST-Umstellung) nach UTC konvertieren -
            // Postgres' "timestamp with time zone" akzeptiert ausschliesslich Offset=0.
            var utc = TimeZoneInfo.ConvertTimeToUtc(localDateTime, ViennaTimeZone);
            return new DateTimeOffset(utc, TimeSpan.Zero);
        }

        return null;
    }

    // Terminale Edikt-Typen laut Seitentitel: Das Verfahren ist beendet oder der Termin
    // aufgehoben - die Liegenschaft ist nicht (mehr) ersteigerbar, selbst wenn die Seite
    // noch ein zukuenftiges Terminfeld fuehrt (z.B. der entfallene Termin unter "Termin:").
    private static readonly string[] ConcludedEdictTitlePrefixes =
    [
        "Entfall des Termins",
        "Zuschlag",
        "Meistbotsverteilung",
        "Einstellung",
        "Aufschiebung",
        "Bietanbot"
    ];

    internal static bool IsConcludedEdictType(string? title) =>
        !string.IsNullOrWhiteSpace(title)
        && ConcludedEdictTitlePrefixes.Any(prefix =>
            title.TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

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

    /// <summary>
    /// Bundesland der Auktion bestimmen. Der Suchfilter [BL]=(code) garantiert das
    /// Bundesland aller Treffer - das ist zuverlaessiger als jede PLZ-Heuristik
    /// (z.B. gehoert 5311 Innerschwand am Mondsee zu OOe, nicht zu Salzburg).
    /// </summary>
    private AustrianState? ResolveState(EdiktDetail detail) =>
        ConfiguredBundeslandState()
        ?? GuessStateFromPostalCode(ExtractPostalCode(detail.PostalCodeAndCity));

    // Codes laut Suchformular der Ediktsdatei (select name="BL")
    private AustrianState? ConfiguredBundeslandState() => scrapingOptions.Value.BundeslandCode switch
    {
        0 => AustrianState.Wien,
        1 => AustrianState.Niederoesterreich,
        2 => AustrianState.Burgenland,
        3 => AustrianState.Oberoesterreich,
        4 => AustrianState.Salzburg,
        5 => AustrianState.Steiermark,
        6 => AustrianState.Kaernten,
        7 => AustrianState.Tirol,
        8 => AustrianState.Vorarlberg,
        _ => null
    };

    private static AustrianState? GuessStateFromPostalCode(string? plz)
    {
        if (string.IsNullOrEmpty(plz)) return null;

        // Grobe Zuordnung ueber PLZ-Leitzonen. Nicht bundeslandscharf (Grenzorte wie das
        // OOe-Mondseeland mit 5xxx bleiben falsch) - nur Fallback ohne Bundesland-Filter.
        return plz[0] switch
        {
            '1' => AustrianState.Wien,
            '2' or '3' => AustrianState.Niederoesterreich,
            '4' => AustrianState.Oberoesterreich,
            '5' => AustrianState.Salzburg,
            // Vorarlberg belegt 6700-6999 (Bludenz, Feldkirch, Dornbirn, Bregenz) - nicht nur 69xx
            '6' => plz.Length >= 2 && plz[1] >= '7' ? AustrianState.Vorarlberg : AustrianState.Tirol,
            '7' => AustrianState.Burgenland,
            '8' => AustrianState.Steiermark,
            // 99xx ist Osttirol (Lienz), der Rest von 9xxx Kaernten
            '9' => plz.StartsWith("99") ? AustrianState.Tirol : AustrianState.Kaernten,
            _ => null
        };
    }

    /// <summary>
    /// Parst das Veröffentlichungsdatum aus dem PublicationDateText.
    /// Typische Formate: "Bekannt gemacht am 15.1.2026", "15.01.2026", etc.
    /// </summary>
    internal static DateTimeOffset? ParsePublicationDate(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        var match = PublicationDatePattern().Match(text);
        if (!match.Success) return null;

        if (DateTime.TryParseExact(
            match.Groups[1].Value,
            ["d.M.yyyy", "dd.MM.yyyy", "d.MM.yyyy", "dd.M.yyyy"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var localDate))
        {
            // Das Edikt nennt nur einen Kalendertag, keine Uhrzeit. Auf Wiener MITTAG verankern
            // statt Mitternacht: Mitternacht Wien ist 22:00 UTC des Vortags, und da Web-SSR und
            // MAUI den Wert als Datum ohne Zeitzonen-Umrechnung ausgeben, waere sonst ueberall
            // der Vortag zu sehen. Mittag haelt den Kalendertag in jeder Anzeige-Zeitzone stabil.
            var viennaNoon = localDate.Date.AddHours(12);
            var utc = TimeZoneInfo.ConvertTimeToUtc(viennaNoon, ViennaTimeZone);
            return new DateTimeOffset(utc, TimeSpan.Zero);
        }

        return null;
    }

    [GeneratedRegex(@"(\d{1,2}\.\d{1,2}\.\d{4})")]
    private static partial Regex PublicationDatePattern();

    [GeneratedRegex(@"am\s+(\d{1,2}\.\d{1,2}\.\d{4})\s+um\s+(\d{1,2}:\d{2})")]
    private static partial Regex DatePattern();

    [GeneratedRegex(@"^(\d{4})\s")]
    private static partial Regex PlzExtractPattern();
}
