using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>Vorlage anlegen oder bearbeiten. Der Name ist eindeutig.</summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class SaveMarketingTemplateHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<SaveMarketingTemplateRequest, SaveMarketingTemplateResponse>
{
    [MediatorHttpPost("/templates/save", OperationId = "SaveMarketingTemplate")]
    public async Task<SaveMarketingTemplateResponse> Handle(SaveMarketingTemplateRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return new SaveMarketingTemplateResponse(false, null, "Bitte einen Namen angeben.");

        if (string.IsNullOrWhiteSpace(request.Subject))
            return new SaveMarketingTemplateResponse(false, null, "Bitte einen Betreff angeben.");

        if (string.IsNullOrWhiteSpace(request.Body))
            return new SaveMarketingTemplateResponse(false, null, "Bitte einen Text angeben.");

        var set = dbContext.Set<MarketingEmailTemplate>();

        var duplicate = await set.AnyAsync(
            x => x.Name == name && x.Id != request.Id,
            cancellationToken);
        if (duplicate)
            return new SaveMarketingTemplateResponse(false, null, "Es gibt bereits eine Vorlage mit diesem Namen.");

        MarketingEmailTemplate template;
        if (request.Id is { } id)
        {
            var existing = await set.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (existing is null)
                return new SaveMarketingTemplateResponse(false, null, "Vorlage nicht gefunden.");

            template = existing;
            template.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            template = new MarketingEmailTemplate
            {
                Name = name,
                Subject = request.Subject,
                Body = request.Body
            };
            set.Add(template);
        }

        template.Name = name;
        template.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        template.Subject = request.Subject.Trim();
        template.Body = request.Body.Trim();
        template.IsActive = request.IsActive;
        template.DisplayOrder = request.DisplayOrder;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SaveMarketingTemplateResponse(true, template.Id, null);
    }
}
