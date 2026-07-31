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
/// Zusatzadresse zu einem Kontakt hinzufuegen. Adresse wird wie die Versand-Adresse
/// normalisiert (lowercase/trim) und muss ueber ALLE Kontakte frei sein - sowohl als
/// Versand- als auch als Zusatzadresse (der Posteingang-Sync ordnet ueber beide zu,
/// eine Adresse darf nie auf zwei Kontakte zeigen).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class AddMarketingContactEmailHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<AddMarketingContactEmailRequest, MarketingContactEmailActionResponse>
{
    [MediatorHttpPost("/contacts/emails/add", OperationId = "AddMarketingContactEmail")]
    public async Task<MarketingContactEmailActionResponse> Handle(AddMarketingContactEmailRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var contact = await dbContext.Set<MarketingContact>()
            .FirstOrDefaultAsync(c => c.Id == request.ContactId, cancellationToken);
        if (contact is null)
            return new MarketingContactEmailActionResponse(false, "Kontakt wurde nicht gefunden.");

        if (!MailAddress.TryCreate(request.Email?.Trim(), out var address))
            return new MarketingContactEmailActionResponse(false, "Die E-Mail-Adresse ist ungültig.");

        var normalized = address.Address.Trim().ToLowerInvariant();

        if (string.Equals(contact.Email, normalized, StringComparison.OrdinalIgnoreCase))
            return new MarketingContactEmailActionResponse(false, "Diese Adresse ist bereits die Versand-Adresse des Kontakts.");

        var usedAsPrimary = await dbContext.Set<MarketingContact>()
            .AnyAsync(c => c.Email == normalized, cancellationToken);
        var usedAsAdditional = await dbContext.Set<MarketingContactEmail>()
            .AnyAsync(a => a.Email == normalized, cancellationToken);
        if (usedAsPrimary || usedAsAdditional)
            return new MarketingContactEmailActionResponse(false, "Diese Adresse ist bereits einem Kontakt zugeordnet.");

        dbContext.Set<MarketingContactEmail>().Add(new MarketingContactEmail
        {
            Id = Guid.NewGuid(),
            ContactId = contact.Id,
            Email = normalized,
            Source = "Manuell"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return new MarketingContactEmailActionResponse(true, null);
    }
}
