using System.Net.Mail;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>
/// Kontakt anlegen (Id=null) oder bearbeiten. E-Mail wird normalisiert (lowercase/trim);
/// der Unique-Index verhindert Dubletten - hier zusaetzlich mit sprechender Fehlermeldung.
/// Die Adresse ist optional: Kontakte aus dem Firmenpool haben zunaechst nur Firma und Ort.
/// Ein Statuswechsel landet als Eintrag in der Kontakt-Historie.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class SaveMarketingContactHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<SaveMarketingContactRequest, SaveMarketingContactResponse>
{
    [MediatorHttpPost("/contacts/save", OperationId = "SaveMarketingContact")]
    public async Task<SaveMarketingContactResponse> Handle(SaveMarketingContactRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        // Mindestens ein identifizierendes Merkmal (WEB-005): voellig leere Kontakte
        // wurden gespeichert und erschienen als "keine E-Mail / Unbekannt"
        if (NullIfEmpty(request.Email) is null
            && NullIfEmpty(request.Name) is null
            && NullIfEmpty(request.Company) is null)
        {
            return new SaveMarketingContactResponse(false, null, "Mindestens E-Mail, Name oder Firma muss angegeben werden.");
        }

        // Adresse ist optional - nur wenn eine angegeben ist, muss sie gueltig und frei sein
        string? normalizedEmail = null;
        var rawEmail = NullIfEmpty(request.Email);
        if (rawEmail is not null)
        {
            if (!MailAddress.TryCreate(rawEmail, out var address))
                return new SaveMarketingContactResponse(false, null, "Die E-Mail-Adresse ist ungültig.");

            normalizedEmail = address.Address.Trim().ToLowerInvariant();

            var duplicate = await dbContext.Set<MarketingContact>()
                .AnyAsync(c => c.Email == normalizedEmail && c.Id != request.Id, cancellationToken);
            if (duplicate)
                return new SaveMarketingContactResponse(false, null, "Ein Kontakt mit dieser E-Mail-Adresse existiert bereits.");
        }

        MarketingContact contact;
        MarketingContactStatus? previousStatus = null;
        if (request.Id is { } id)
        {
            var existing = await dbContext.Set<MarketingContact>()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (existing is null)
                return new SaveMarketingContactResponse(false, null, "Kontakt wurde nicht gefunden.");
            contact = existing;
            previousStatus = existing.Status;
        }
        else
        {
            contact = new MarketingContact
            {
                Id = Guid.NewGuid(),
                Source = "Manuell"
            };
            dbContext.Set<MarketingContact>().Add(contact);
        }

        contact.Email = normalizedEmail;
        contact.Name = NullIfEmpty(request.Name);
        contact.Company = NullIfEmpty(request.Company);
        contact.Phone = NullIfEmpty(request.Phone);
        contact.City = NullIfEmpty(request.City);
        contact.ContactType = request.ContactType;
        contact.Status = request.Status;
        contact.Notes = NullIfEmpty(request.Notes);

        // Statuswechsel ueber das Formular gehoert genauso in die Historie wie einer
        // ueber die Aktivitaets-Erfassung
        if (previousStatus is { } from && from != request.Status)
        {
            dbContext.Set<MarketingActivity>().Add(
                MarketingActivity.StatusChange(contact.Id, from, request.Status, DateTimeOffset.UtcNow));

            // Endstatus leert eine offene Wiedervorlage (vgl. LogMarketingActivityHandler)
            if (request.Status.IsClosed())
                contact.NextFollowUpAt = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new SaveMarketingContactResponse(true, contact.Id, null);
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
