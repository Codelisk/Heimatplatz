using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>
/// Loescht einen Kontakt endgueltig - Versand-Historie und Rueckmeldungen gehen per
/// FK-Kaskade mit (DSGVO: alle personenbezogenen Daten des Kontakts in einem Schritt).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class DeleteMarketingContactHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard,
    ILogger<DeleteMarketingContactHandler> logger
) : IRequestHandler<DeleteMarketingContactRequest, DeleteMarketingContactResponse>
{
    [MediatorHttpDelete("/contacts/{Id}", OperationId = "DeleteMarketingContact")]
    public async Task<DeleteMarketingContactResponse> Handle(DeleteMarketingContactRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var contact = await dbContext.Set<MarketingContact>()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (contact is null)
            return new DeleteMarketingContactResponse(false);

        dbContext.Set<MarketingContact>().Remove(contact);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("[Marketing] Kontakt {Email} samt Historie gelöscht", contact.Email);
        return new DeleteMarketingContactResponse(true);
    }
}
