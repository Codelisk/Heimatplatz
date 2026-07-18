using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.PropertyDrafts.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.PropertyDrafts.Data.Entities;
using Heimatplatz.Api.Features.PropertyDrafts.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.PropertyDrafts.Handlers;

/// <summary>
/// Listet die Inserat-Entwuerfe des angemeldeten Verkaeufers (neueste zuerst).
/// Liefert nur die Summary-Spalten - der Payload wird erst beim Fortsetzen geladen.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/property-drafts")]
public class GetPropertyDraftsHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GetPropertyDraftsRequest, GetPropertyDraftsResponse>
{
    [MediatorHttpGet("/", OperationId = "GetPropertyDrafts", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireSeller])]
    public async Task<GetPropertyDraftsResponse> Handle(
        GetPropertyDraftsRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.GetRequiredUserId();

        var drafts = await dbContext.Set<PropertyDraft>()
            // Bereits veroeffentlichte Entwuerfe (warten nur noch auf die Text-Nachlieferung
            // des Beschreibungs-Jobs) gehoeren nicht mehr in die Liste
            .Where(d => d.UserId == userId && d.PublishedPropertyId == null)
            .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
            .Select(d => new PropertyDraftListItemDto(
                d.Id,
                d.Title,
                d.Type,
                d.StepIndex,
                d.FirstImageUrl,
                d.UpdatedAt ?? d.CreatedAt))
            .ToListAsync(cancellationToken);

        return new GetPropertyDraftsResponse(drafts);
    }
}
