using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>E-Mail-Vorlagen fuer Auswahl (aktive) und Verwaltung (alle).</summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class GetMarketingTemplatesHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<GetMarketingTemplatesRequest, GetMarketingTemplatesResponse>
{
    [MediatorHttpGet("/templates", OperationId = "GetMarketingTemplates")]
    public async Task<GetMarketingTemplatesResponse> Handle(GetMarketingTemplatesRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var query = dbContext.Set<MarketingEmailTemplate>().AsNoTracking();

        if (!request.IncludeInactive)
            query = query.Where(x => x.IsActive);

        var templates = await query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new MarketingTemplateDto(
                x.Id, x.Name, x.Description, x.Subject, x.Body,
                x.IsActive, x.DisplayOrder, x.CreatedAt))
            .ToListAsync(cancellationToken);

        return new GetMarketingTemplatesResponse(templates);
    }
}
