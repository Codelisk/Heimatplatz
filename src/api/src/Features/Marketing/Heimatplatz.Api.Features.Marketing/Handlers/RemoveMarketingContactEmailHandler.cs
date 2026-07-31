using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>
/// Zusatzadresse eines Kontakts entfernen. Bereits zugeordnete Posteingang-Mails behalten
/// ihren Kontakt (die Zuordnung haengt an der Inbound-Zeile, nicht an der Adresse) -
/// entfernt wird nur die kuenftige Zuordnung ueber diese Adresse.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class RemoveMarketingContactEmailHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<RemoveMarketingContactEmailRequest, MarketingContactEmailActionResponse>
{
    [MediatorHttpPost("/contacts/emails/remove", OperationId = "RemoveMarketingContactEmail")]
    public async Task<MarketingContactEmailActionResponse> Handle(RemoveMarketingContactEmailRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var normalized = request.Email?.Trim().ToLowerInvariant() ?? "";
        var entry = await dbContext.Set<MarketingContactEmail>()
            .FirstOrDefaultAsync(a => a.ContactId == request.ContactId && a.Email == normalized, cancellationToken);
        if (entry is null)
            return new MarketingContactEmailActionResponse(false, "Die Adresse wurde beim Kontakt nicht gefunden.");

        dbContext.Set<MarketingContactEmail>().Remove(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new MarketingContactEmailActionResponse(true, null);
    }
}
