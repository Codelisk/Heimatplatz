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
/// Polling-Endpoint fuer den Fortschritt der KI-Beschreibungs-Generierung eines Entwurfs.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/property-drafts")]
public class GetDraftDescriptionHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GetDraftDescriptionRequest, GetDraftDescriptionResponse>
{
    [MediatorHttpGet("/{Id}/description", OperationId = "GetDraftDescription", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireSeller])]
    public async Task<GetDraftDescriptionResponse> Handle(
        GetDraftDescriptionRequest request,
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

        return new GetDraftDescriptionResponse(
            draft.DescriptionStatus,
            draft.GeneratedDescription,
            draft.DescriptionError);
    }
}
