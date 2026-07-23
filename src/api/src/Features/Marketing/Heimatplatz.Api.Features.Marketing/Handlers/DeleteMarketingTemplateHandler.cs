using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>Loescht eine E-Mail-Vorlage. Versendete Mails haengen nicht an der Vorlage.</summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class DeleteMarketingTemplateHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<DeleteMarketingTemplateRequest, DeleteMarketingTemplateResponse>
{
    [MediatorHttpDelete("/templates/{Id}", OperationId = "DeleteMarketingTemplate")]
    public async Task<DeleteMarketingTemplateResponse> Handle(DeleteMarketingTemplateRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var template = await dbContext.Set<MarketingEmailTemplate>()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (template is null)
            return new DeleteMarketingTemplateResponse(false);

        dbContext.Set<MarketingEmailTemplate>().Remove(template);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteMarketingTemplateResponse(true);
    }
}
