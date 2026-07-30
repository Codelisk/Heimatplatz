using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Firmenbuch.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>
/// Uebernimmt ausgewaehlte Firmenpool-Firmen als Kontakte mit Status "Zu kontaktieren".
/// Je Firma wird der volle Datensatz live beim Firmenpool geholt - erst mit der Uebernahme
/// entsteht ueberhaupt ein Datensatz in der Heimatplatz-Datenbank. Der Kontakt startet ohne
/// E-Mail-Adresse (das Firmenbuch fuehrt keine Kontaktdaten); Adresse, Rechtsform und
/// Geschaeftsfuehrung wandern in die Startnotiz.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class AddMarketingLeadsHandler(
    AppDbContext dbContext,
    IFirmenpoolApiClient firmenpool,
    IAdminAccessGuard accessGuard
) : IRequestHandler<AddMarketingLeadsRequest, AddMarketingLeadsResponse>
{
    // Je Firma ein Live-Abruf beim Firmenpool (~200ms) - deshalb deutlich enger als die
    // frueheren 500 aus Spiegel-Zeiten. Eine Poolseite (max. 200) uebersteigt das bewusst:
    // Massenuebernahme in Etappen statt minutenlangem Request.
    private const int MaxPerRequest = 50;
    private const string SourceName = "Firmenbuch";

    [MediatorHttpPost("/lead-pool/add", OperationId = "AddMarketingLeads")]
    public async Task<AddMarketingLeadsResponse> Handle(AddMarketingLeadsRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var fnrs = (request.Fnrs ?? [])
            .Select(f => f?.Trim())
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => f!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (fnrs.Count == 0)
            return new AddMarketingLeadsResponse(false, 0, 0, "Keine Firma ausgewaehlt.");

        if (fnrs.Count > MaxPerRequest)
            return new AddMarketingLeadsResponse(false, 0, 0, $"Maximal {MaxPerRequest} Firmen pro Uebernahme.");

        // Bereits uebernommene Firmenbuchnummern - macht die Uebernahme idempotent,
        // ohne pro Firma eine eigene Abfrage zu fahren
        var existing = await dbContext.Set<MarketingContact>()
            .Where(x => x.FirmenbuchFnr != null && fnrs.Contains(x.FirmenbuchFnr))
            .Select(x => x.FirmenbuchFnr!)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var added = 0;
        var skipped = 0;

        foreach (var fnr in fnrs)
        {
            if (existing.Contains(fnr, StringComparer.OrdinalIgnoreCase))
            {
                skipped++;
                continue;
            }

            var company = await firmenpool.GetCompanyDetailAsync(fnr, cancellationToken);
            if (company is null)
            {
                // FNR dort unbekannt (Tippfehler, Firma ausserhalb OOe) - kein Abbruch,
                // die uebrigen Firmen sollen trotzdem uebernommen werden.
                skipped++;
                continue;
            }

            var contact = new MarketingContact
            {
                Id = Guid.NewGuid(),
                // Firmenname ist im Firmenbuch bis 500 Zeichen lang, das Kontaktfeld
                // fasst 200 - abschneiden statt den Insert scheitern zu lassen
                Company = Truncate(company.Name, 200),
                City = Truncate(company.Ort ?? company.Sitz, 100),
                // Kanonische FNR der Quelle, nicht die Eingabe - schuetzt den
                // Unique-Index vor Schreibweisen-Dubletten
                FirmenbuchFnr = company.Fnr,
                ContactType = MarketingContactType.Broker,
                Status = MarketingContactStatus.ToContact,
                Source = SourceName,
                Notes = BuildNote(company)
            };

            contact.Activities.Add(MarketingActivity.StatusChange(
                contact.Id, null, MarketingContactStatus.ToContact, now, "Aus dem Firmenpool uebernommen"));

            dbContext.Set<MarketingContact>().Add(contact);
            added++;
        }

        if (added == 0)
            return new AddMarketingLeadsResponse(true, 0, skipped, null);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Race: eine parallele Uebernahme (oder ein Doppel-Klick) hat eine der Firmen
            // zwischen Existenz-Pruefung und Insert bereits angelegt - der partielle
            // Unique-Index auf FirmenbuchFnr schlaegt zu. Statt 500 eine sprechende Meldung;
            // der Pool zeigt nach dem Neuladen den aktuellen Stand.
            return new AddMarketingLeadsResponse(false, 0, 0,
                "Einige Firmen wurden gerade zeitgleich uebernommen. Bitte die Seite neu laden.");
        }

        return new AddMarketingLeadsResponse(true, added, skipped, null);
    }

    /// <summary>
    /// Startnotiz aus dem Firmenpool-Datensatz: FNR, Rechtsform, Gericht, Adresse und
    /// aktive Geschaeftsfuehrung - nichts davon steht sonst am Kontakt.
    /// </summary>
    private static string BuildNote(FirmenpoolCompanyDetail company)
    {
        var parts = new List<string> { $"Firmenbuch: {company.Fnr}" };

        if (!string.IsNullOrWhiteSpace(company.RechtsformText))
            parts.Add(company.RechtsformText);

        if (!string.IsNullOrWhiteSpace(company.GerichtText))
            parts.Add(company.GerichtText);

        var adresse = string.Join(" ", new[] { company.Strasse, company.Hausnummer }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        var ort = string.Join(" ", new[] { company.Plz, company.Ort }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        var anschrift = string.Join(", ", new[] { adresse, ort }.Where(x => x.Length > 0));
        if (anschrift.Length > 0)
            parts.Add(anschrift);

        var geschaeftsfuehrung = company.Funktionaere
            .Where(f => f.Aktiv)
            .Select(f => f.Name)
            .Distinct()
            .Take(3)
            .ToList();
        if (geschaeftsfuehrung.Count > 0)
            parts.Add($"GF: {string.Join(", ", geschaeftsfuehrung)}");

        return string.Join(" | ", parts);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
