using System.Net.Mail;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>
/// Kontakt anlegen (Id=null) oder bearbeiten. E-Mail wird normalisiert (lowercase/trim);
/// der Unique-Index verhindert Dubletten - hier zusaetzlich mit sprechender Fehlermeldung.
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

        if (!MailAddress.TryCreate(request.Email?.Trim(), out var address))
            return new SaveMarketingContactResponse(false, null, "Die E-Mail-Adresse ist ungültig.");

        var normalizedEmail = address.Address.Trim().ToLowerInvariant();

        var duplicate = await dbContext.Set<MarketingContact>()
            .AnyAsync(c => c.Email == normalizedEmail && c.Id != request.Id, cancellationToken);
        if (duplicate)
            return new SaveMarketingContactResponse(false, null, "Ein Kontakt mit dieser E-Mail-Adresse existiert bereits.");

        MarketingContact contact;
        if (request.Id is { } id)
        {
            var existing = await dbContext.Set<MarketingContact>()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (existing is null)
                return new SaveMarketingContactResponse(false, null, "Kontakt wurde nicht gefunden.");
            contact = existing;
        }
        else
        {
            contact = new MarketingContact
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                Source = "Manuell"
            };
            dbContext.Set<MarketingContact>().Add(contact);
        }

        contact.Email = normalizedEmail;
        contact.Name = NullIfEmpty(request.Name);
        contact.Company = NullIfEmpty(request.Company);
        contact.Phone = NullIfEmpty(request.Phone);
        contact.ContactType = request.ContactType;
        contact.Status = request.Status;
        contact.Notes = NullIfEmpty(request.Notes);

        await dbContext.SaveChangesAsync(cancellationToken);
        return new SaveMarketingContactResponse(true, contact.Id, null);
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
