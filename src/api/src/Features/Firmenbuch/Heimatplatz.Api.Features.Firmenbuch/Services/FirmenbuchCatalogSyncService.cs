using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Firmenbuch.Configuration;
using Heimatplatz.Api.Features.Firmenbuch.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny;

namespace Heimatplatz.Api.Features.Firmenbuch.Services;

/// <summary>
/// Spiegelt den Firmenbuch-Katalog aus der Firmenpool-API (seitenweiser Abzug, Upsert per FNR).
/// Den Praefix-Crawl gegen die amtliche SUCHEFIRMA-Operation macht der Firmenpool selbst -
/// hier kommt nur noch dessen konsolidierter Bestand an (inkl. Delta-Pflege und der
/// Kurzwort-Namen, an denen der fruehere eigene Crawl scheiterte).
/// Speichert nach JEDER Seite, damit Teilfortschritt sichtbar bleibt und ein Abbruch
/// bereits geladene Daten nicht verliert. Eintraege werden nie geloescht.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class FirmenbuchCatalogSyncService(
    IFirmenpoolApiClient client,
    AppDbContext dbContext,
    IOptions<FirmenpoolOptions> options,
    ILogger<FirmenbuchCatalogSyncService> logger
) : IFirmenbuchCatalogSyncService
{
    public async Task<FirmenbuchCatalogSyncResult> SyncAsync(CancellationToken ct = default)
    {
        logger.LogInformation(
            "Starte Firmenbuch-Katalog-Spiegel aus dem Firmenpool ({BaseUrl})", options.Value.BaseUrl);

        var created = 0;
        var updated = 0;
        var unchanged = 0;
        var pages = 0;
        var errors = 0;
        var errorMessages = new List<string>();

        // Kompletter FNR-Index einmalig in den Speicher - der Katalog ist Stammdaten-artig
        // (Strings, kein Blob-Ballast), das haelt auch einige 100k Eintraege problemlos.
        var existingByFnr = await dbContext.Set<FirmenbuchCompany>()
            .ToDictionaryAsync(c => c.Fnr, ct);

        var pageSize = Math.Clamp(options.Value.PageSize, 1, 200);
        var page = 1;
        var expectedTotal = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            FirmenpoolCompanyPage result;
            try
            {
                result = await client.GetCompaniesAsync(page, pageSize, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Eine gescheiterte Seite bricht den Lauf ab statt sie zu ueberspringen -
                // ein Luecken-Spiegel waere schlechter als ein alter, der Upsert macht den
                // naechsten Lauf ohnehin vollstaendig.
                errors++;
                errorMessages.Add($"Seite {page} fehlgeschlagen: {ex.Message}");
                logger.LogError(ex, "Firmenbuch-Spiegel: Seite {Page} fehlgeschlagen - Lauf abgebrochen", page);
                break;
            }

            pages++;
            expectedTotal = result.TotalCount;

            foreach (var item in result.Items)
            {
                if (existingByFnr.TryGetValue(item.Fnr, out var existing))
                {
                    if (Apply(existing, item)) updated++;
                    else unchanged++;
                }
                else
                {
                    var entity = new FirmenbuchCompany
                    {
                        Fnr = item.Fnr,
                        Name = item.Name,
                        FirstSeenAt = item.FirstSeenAt,
                        LastSeenAt = item.LastSeenAt
                    };
                    Apply(entity, item);
                    dbContext.Set<FirmenbuchCompany>().Add(entity);
                    existingByFnr[item.Fnr] = entity;
                    created++;
                }
            }

            // Inkrementell speichern: Teilfortschritt sofort sichtbar, Abbruch verliert nichts
            await dbContext.SaveChangesAsync(ct);

            if (result.Items.Count < pageSize)
                break;

            page++;
        }

        logger.LogInformation(
            "Firmenbuch-Katalog-Spiegel abgeschlossen: {Created} neu, {Updated} aktualisiert, {Unchanged} unveraendert, {Pages} Seiten (Quelle meldet {Total} Firmen), {Errors} Fehler",
            created, updated, unchanged, pages, expectedTotal, errors);

        return new FirmenbuchCatalogSyncResult(created, updated, unchanged, pages, errors, errorMessages);
    }

    /// <summary>Uebertraegt einen Firmenpool-Stammsatz auf die Entity; true wenn sich fachlich etwas geaendert hat.</summary>
    private static bool Apply(FirmenbuchCompany entity, FirmenpoolCompanyItem item)
    {
        var changed =
            entity.Name != item.Name
            || entity.Status != item.Status
            || entity.Sitz != item.Sitz
            || entity.RechtsformCode != item.RechtsformCode
            || entity.RechtsformText != item.RechtsformText
            || entity.Rechtseigenschaft != item.Rechtseigenschaft
            || entity.GerichtCode != item.GerichtCode
            || entity.GerichtText != item.GerichtText
            || entity.SourceOrtNr != item.SourceOrtNr;

        entity.Name = item.Name;
        entity.Status = item.Status;
        entity.Sitz = item.Sitz;
        entity.RechtsformCode = item.RechtsformCode;
        entity.RechtsformText = item.RechtsformText;
        entity.Rechtseigenschaft = item.Rechtseigenschaft;
        entity.GerichtCode = item.GerichtCode;
        entity.GerichtText = item.GerichtText;
        entity.SourceOrtNr = item.SourceOrtNr;

        // Sichtungszeitpunkte der QUELLE uebernehmen: seit dem Umstieg auf den Firmenpool
        // bedeuten First-/LastSeenAt, wann DESSEN Crawl die Firma gesehen hat - das ist die
        // Aussage, die die Intern-Detailseite unter "Datenquelle & Aktualitaet" braucht.
        entity.FirstSeenAt = item.FirstSeenAt;
        entity.LastSeenAt = item.LastSeenAt;

        return changed;
    }
}
