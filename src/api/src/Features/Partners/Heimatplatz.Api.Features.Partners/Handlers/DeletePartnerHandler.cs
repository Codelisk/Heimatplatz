using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Partners.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Partners.Data.Entities;
using Heimatplatz.Api.Features.Properties.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Partners.Handlers;

/// <summary>
/// Loescht einen Partner endgueltig und raeumt ein hochgeladenes Logo mit ab
/// (best effort - ein fehlgeschlagener Datei-Delete blockiert das Loeschen nicht).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/partners")]
public class DeletePartnerHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard,
    IPropertyImageService imageService,
    ILogger<DeletePartnerHandler> logger
) : IRequestHandler<DeletePartnerRequest, DeletePartnerResponse>
{
    [MediatorHttpPost("/delete", OperationId = "DeletePartner")]
    public async Task<DeletePartnerResponse> Handle(DeletePartnerRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var partner = await dbContext.Set<Partner>()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (partner == null)
            return new DeletePartnerResponse(false, "Der Partner wurde nicht gefunden (eventuell bereits gelöscht).");

        if (!string.IsNullOrWhiteSpace(partner.LogoUrl))
        {
            try
            {
                // DeleteImageAsync ignoriert Nicht-Upload-URLs selbst (uploads/-Guard)
                await imageService.DeleteImageAsync(partner.LogoUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[Admin] Logo zu Partner {Name} konnte nicht geloescht werden: {Url}",
                    partner.Name, partner.LogoUrl);
            }
        }

        dbContext.Set<Partner>().Remove(partner);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("[Admin] Partner geloescht: {Name}", partner.Name);

        return new DeletePartnerResponse(true, null);
    }
}
