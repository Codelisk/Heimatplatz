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
/// Upsert eines Inserat-Entwurfs. Bewusst lenient: Entwuerfe duerfen beliebig
/// unvollstaendig sein - die volle Validierung passiert erst beim Publish
/// (dort via CreatePropertyHandler).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/property-drafts")]
public class SavePropertyDraftHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<SavePropertyDraftRequest, SavePropertyDraftResponse>
{
    const int MaxPayloadLength = 64 * 1024;
    const int MaxDraftsPerUser = 10;
    const int MaxStepIndex = 4;

    [MediatorHttpPost("/", OperationId = "SavePropertyDraft", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireSeller])]
    public async Task<SavePropertyDraftResponse> Handle(
        SavePropertyDraftRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.GetRequiredUserId();

        var payloadJson = DraftPayloadSerializer.Serialize(request.Data);
        if (payloadJson.Length > MaxPayloadLength)
            throw new ArgumentException("Der Entwurf ist zu gross.");

        PropertyDraft draft;
        if (request.Id is { } id)
        {
            draft = await dbContext.Set<PropertyDraft>()
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
                ?? throw new KeyNotFoundException($"Entwurf mit ID {id} nicht gefunden.");

            if (draft.UserId != userId)
                throw new UnauthorizedAccessException("Sie haben keine Berechtigung fuer diesen Entwurf.");
        }
        else
        {
            var draftCount = await dbContext.Set<PropertyDraft>()
                .CountAsync(d => d.UserId == userId, cancellationToken);
            if (draftCount >= MaxDraftsPerUser)
                throw new ArgumentException($"Maximal {MaxDraftsPerUser} Entwuerfe moeglich - bitte zuerst einen Entwurf loeschen oder veroeffentlichen.");

            draft = new PropertyDraft { Id = Guid.NewGuid(), UserId = userId };
            dbContext.Set<PropertyDraft>().Add(draft);
        }

        // Summary-Spalten aus dem Payload ableiten (nur fuer die Listen-Anzeige)
        var title = request.Data.Title?.Trim();
        draft.Title = string.IsNullOrEmpty(title) ? null : title[..Math.Min(title.Length, 200)];
        draft.Type = request.Data.Type;
        draft.StepIndex = Math.Clamp(request.Data.StepIndex, 0, MaxStepIndex);
        draft.FirstImageUrl = request.Data.ImageUrls?.FirstOrDefault();
        draft.SchemaVersion = request.Data.SchemaVersion;
        draft.PayloadJson = payloadJson;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SavePropertyDraftResponse(draft.Id, draft.UpdatedAt ?? draft.CreatedAt);
    }
}
