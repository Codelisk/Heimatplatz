using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Firmenbuch.Data.Entities;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>
/// Uebernimmt ausgewaehlte Firmenbuch-Firmen als Kontakte mit Status "Zu kontaktieren".
/// Der Kontakt entsteht ohne E-Mail-Adresse - das Firmenbuch fuehrt keine Kontaktdaten,
/// die kommen beim Telefonat dazu.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class AddMarketingLeadsHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<AddMarketingLeadsRequest, AddMarketingLeadsResponse>
{
    private const int MaxPerRequest = 500;
    private const string SourceName = "Firmenbuch";

    [MediatorHttpPost("/lead-pool/add", OperationId = "AddMarketingLeads")]
    public async Task<AddMarketingLeadsResponse> Handle(AddMarketingLeadsRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var ids = request.FirmenbuchCompanyIds?.Distinct().ToList() ?? [];
        if (ids.Count == 0)
            return new AddMarketingLeadsResponse(false, 0, 0, "Keine Firma ausgewaehlt.");

        if (ids.Count > MaxPerRequest)
            return new AddMarketingLeadsResponse(false, 0, 0, $"Maximal {MaxPerRequest} Firmen pro Uebernahme.");

        var companies = await dbContext.Set<FirmenbuchCompany>()
            .AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);

        if (companies.Count == 0)
            return new AddMarketingLeadsResponse(false, 0, 0, "Die ausgewaehlten Firmen wurden nicht gefunden.");

        // Bereits uebernommene Firmenbuchnummern - macht die Uebernahme idempotent,
        // ohne pro Firma eine eigene Abfrage zu fahren
        var fnrs = companies.Select(c => c.Fnr).ToList();
        var existing = await dbContext.Set<MarketingContact>()
            .Where(x => x.FirmenbuchFnr != null && fnrs.Contains(x.FirmenbuchFnr))
            .Select(x => x.FirmenbuchFnr!)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var added = 0;

        foreach (var company in companies)
        {
            if (existing.Contains(company.Fnr))
                continue;

            var contact = new MarketingContact
            {
                // Firmenname ist im Firmenbuch bis 500 Zeichen lang, das Kontaktfeld
                // fasst 200 - abschneiden statt den Insert scheitern zu lassen
                Company = Truncate(company.Name, 200),
                City = Truncate(company.Sitz, 100),
                FirmenbuchFnr = company.Fnr,
                ContactType = MarketingContactType.Broker,
                Status = MarketingContactStatus.ToContact,
                Source = SourceName,
                Notes = BuildNote(company)
            };

            contact.Activities.Add(new MarketingActivity
            {
                Type = MarketingActivityType.StatusChange,
                StatusTo = MarketingContactStatus.ToContact,
                Notes = "Aus dem Firmenpool uebernommen",
                OccurredAt = now
            });

            dbContext.Set<MarketingContact>().Add(contact);
            added++;
        }

        if (added > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        return new AddMarketingLeadsResponse(true, added, companies.Count - added, null);
    }

    /// <summary>Rechtsform und Firmenbuchnummer als Startnotiz - beides steht sonst nirgends am Kontakt.</summary>
    private static string BuildNote(FirmenbuchCompany company)
    {
        var parts = new List<string> { $"Firmenbuch: {company.Fnr}" };

        if (!string.IsNullOrWhiteSpace(company.RechtsformText))
            parts.Add(company.RechtsformText);

        if (!string.IsNullOrWhiteSpace(company.GerichtText))
            parts.Add(company.GerichtText);

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
