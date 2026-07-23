using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Heimatplatz.Api.Features.Marketing.Services;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>
/// Liefert eine Vorlage mit aus dem Kontakt befuellten Platzhaltern - das Gegenstueck zur
/// KI-Generierung auf der Schreiben-Seite.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class RenderMarketingTemplateHandler(
    AppDbContext dbContext,
    IMarketingTemplateRenderer renderer,
    IAdminAccessGuard accessGuard
) : IRequestHandler<RenderMarketingTemplateRequest, RenderMarketingTemplateResponse>
{
    [MediatorHttpPost("/templates/render", OperationId = "RenderMarketingTemplate")]
    public async Task<RenderMarketingTemplateResponse> Handle(RenderMarketingTemplateRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var template = await dbContext.Set<MarketingEmailTemplate>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.TemplateId, cancellationToken);

        if (template is null)
            return new RenderMarketingTemplateResponse(false, null, null, "Vorlage nicht gefunden.");

        MarketingContact? contact = null;
        if (request.ContactId is { } contactId)
        {
            contact = await dbContext.Set<MarketingContact>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == contactId, cancellationToken);
        }

        var rendered = renderer.Render(template, contact);

        return new RenderMarketingTemplateResponse(true, rendered.Subject, rendered.Body, null);
    }
}
