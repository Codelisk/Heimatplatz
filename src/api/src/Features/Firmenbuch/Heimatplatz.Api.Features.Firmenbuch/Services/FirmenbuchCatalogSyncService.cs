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
/// Baut den Firmenbuch-Katalog ueber die SUCHEFIRMA-Operation auf. Die Schnittstelle deckelt
/// jede Antwort bei 1000 Treffern (ohne "abgeschnitten"-Flag), daher wird der Namensraum
/// adaptiv partitioniert: Start mit einstelligen Praefixen ("a*", "b*", ...); liefert ein
/// Praefix exakt das Limit, wird er verfeinert ("aa*", "ab*", ...) bis unter dem Deckel.
/// Die (moeglicherweise abgeschnittenen) Treffer eines gedeckelten Praefixes werden trotzdem
/// gespeichert - die Verfeinerungen liefern sie erneut, der Upsert ist idempotent.
/// Speichert nach JEDER Suchanfrage (Teilfortschritt sichtbar, Abbruch verliert nichts -
/// gleiches Muster wie der WKO-Sync seit 22.7.2026).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class FirmenbuchCatalogSyncService(
    IFirmenbuchHvdClient client,
    AppDbContext dbContext,
    IOptions<FirmenbuchHvdOptions> options,
    ILogger<FirmenbuchCatalogSyncService> logger
) : IFirmenbuchCatalogSyncService
{
    /// <summary>Serverseitiger Antwort-Deckel der SUCHEFIRMA-Operation (live verifiziert 22.7.2026).</summary>
    internal const int ResponseCap = 1000;

    /// <summary>
    /// Zeichen fuer Praefix-Start und -Verfeinerung. Firmennamen, deren Anfang KEINES dieser
    /// Zeichen enthaelt (exotische Sonderzeichen), koennen nur ueber einen Praefix gefunden
    /// werden, in dessen 1000er-Fenster sie fallen - Restluecke bewusst akzeptiert und geloggt.
    /// </summary>
    internal const string PrefixAlphabet = "abcdefghijklmnopqrstuvwxyz0123456789äöü &-.'\"/(+,!";

    private const int MaxPrefixLength = 6;

    public async Task<FirmenbuchCatalogSyncResult> SyncAsync(string ortNr, CancellationToken ct = default)
    {
        if (!client.IsConfigured)
        {
            return new FirmenbuchCatalogSyncResult(0, 0, 0, 0, 0, 1,
                ["Kein Firmenbuch-API-Key konfiguriert (JustizOnline:IwgApiKey) - Katalog-Sync uebersprungen."]);
        }

        logger.LogInformation("Starte Firmenbuch-Katalog-Sync (OrtNr='{OrtNr}')", ortNr);

        var created = 0;
        var updated = 0;
        var unchanged = 0;
        var queries = 0;
        var capped = 0;
        var errors = 0;
        var errorMessages = new List<string>();
        var now = DateTimeOffset.UtcNow;

        // Kompletter FNR-Index einmalig in den Speicher - der Katalog ist Stammdaten-artig
        // (Strings, kein Blob-Ballast), das haelt auch einige 100k Eintraege problemlos.
        var existingByFnr = await dbContext.Set<FirmenbuchCompany>()
            .ToDictionaryAsync(c => c.Fnr, ct);

        var processedThisRun = new HashSet<string>();
        var queue = new Queue<string>(PrefixAlphabet.Select(c => c.ToString()));

        while (queue.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var prefix = queue.Dequeue();

            var results = await client.SearchAsync(prefix + "*", ortNr, ct);
            queries++;

            if (results == null)
            {
                // Fault/Netzwerkfehler ist bereits geloggt; Praefix nicht verfeinern (wir wissen
                // nicht ob er ueberhaupt Treffer hat), aber als Fehler zaehlen fuer den Report.
                errors++;
                errorMessages.Add($"Suche fuer Praefix '{prefix}*' fehlgeschlagen (siehe Log)");
            }
            else
            {
                foreach (var result in results)
                {
                    var firstTouchThisRun = processedThisRun.Add(result.Fnr);

                    if (existingByFnr.TryGetValue(result.Fnr, out var existing))
                    {
                        var changed = ApplyResult(existing, result, ortNr);
                        existing.LastSeenAt = now;
                        if (firstTouchThisRun)
                        {
                            if (changed) updated++;
                            else unchanged++;
                        }
                    }
                    else
                    {
                        var entity = new FirmenbuchCompany
                        {
                            Fnr = result.Fnr,
                            Name = result.Name,
                            FirstSeenAt = now,
                            LastSeenAt = now
                        };
                        ApplyResult(entity, result, ortNr);
                        dbContext.Set<FirmenbuchCompany>().Add(entity);
                        existingByFnr[result.Fnr] = entity;
                        created++;
                    }
                }

                if (results.Count >= ResponseCap)
                {
                    if (prefix.Length < MaxPrefixLength)
                    {
                        foreach (var c in PrefixAlphabet)
                            queue.Enqueue(prefix + c);
                    }
                    else
                    {
                        capped++;
                        logger.LogWarning(
                            "Firmenbuch-Katalog: Praefix '{Prefix}*' liefert auch bei maximaler Tiefe {MaxLen} noch {Cap}+ Treffer - Rest dieses Praefixes nicht vollstaendig erfassbar",
                            prefix, MaxPrefixLength, ResponseCap);
                    }
                }

                // Inkrementell speichern: Teilfortschritt sofort sichtbar, Abbruch verliert nichts
                await dbContext.SaveChangesAsync(ct);
            }

            if (queue.Count > 0)
                await Task.Delay(options.Value.DelayBetweenRequestsMs, ct);
        }

        logger.LogInformation(
            "Firmenbuch-Katalog-Sync abgeschlossen (OrtNr='{OrtNr}'): {Created} neu, {Updated} aktualisiert, {Unchanged} unveraendert, {Queries} Suchanfragen, {Capped} nicht aufloesbare Praefixe, {Errors} Fehler",
            ortNr, created, updated, unchanged, queries, capped, errors);

        return new FirmenbuchCatalogSyncResult(created, updated, unchanged, queries, capped, errors, errorMessages);
    }

    /// <summary>Uebertraegt einen Suchtreffer auf die Entity; true wenn sich fachlich etwas geaendert hat.</summary>
    private static bool ApplyResult(FirmenbuchCompany entity, FirmenbuchSearchResult result, string ortNr)
    {
        var sourceOrtNr = string.IsNullOrWhiteSpace(ortNr) ? null : ortNr;
        var changed =
            entity.Name != result.Name
            || entity.Status != result.Status
            || entity.Sitz != result.Sitz
            || entity.RechtsformCode != result.RechtsformCode
            || entity.RechtsformText != result.RechtsformText
            || entity.Rechtseigenschaft != result.Rechtseigenschaft
            || entity.GerichtCode != result.GerichtCode
            || entity.GerichtText != result.GerichtText;

        entity.Name = result.Name;
        entity.Status = result.Status;
        entity.Sitz = result.Sitz;
        entity.RechtsformCode = result.RechtsformCode;
        entity.RechtsformText = result.RechtsformText;
        entity.Rechtseigenschaft = result.Rechtseigenschaft;
        entity.GerichtCode = result.GerichtCode;
        entity.GerichtText = result.GerichtText;
        entity.SourceOrtNr = sourceOrtNr ?? entity.SourceOrtNr;

        return changed;
    }
}
