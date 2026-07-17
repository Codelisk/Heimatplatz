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
/// Laedt einen einzelnen Entwurf inklusive Payload - zum Fortsetzen im Wizard.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/property-drafts")]
public class GetPropertyDraftHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GetPropertyDraftRequest, GetPropertyDraftResponse>
{
    [MediatorHttpGet("/{Id}", OperationId = "GetPropertyDraft", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireSeller])]
    public async Task<GetPropertyDraftResponse> Handle(
        GetPropertyDraftRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.GetRequiredUserId();

        var draft = await dbContext.Set<PropertyDraft>()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Entwurf mit ID {request.Id} nicht gefunden.");

        if (draft.UserId != userId)
            throw new UnauthorizedAccessException("Sie haben keine Berechtigung fuer diesen Entwurf.");

        var data = DraftPayloadSerializer.Deserialize(draft.PayloadJson);

        return new GetPropertyDraftResponse(
            draft.Id,
            data,
            draft.UpdatedAt ?? draft.CreatedAt,
            draft.DescriptionStatus,
            draft.DescriptionKeywords,
            draft.GeneratedDescription,
            draft.DescriptionError);
    }
}
