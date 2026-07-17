using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Services;
using Heimatplatz.Api.Features.PropertyDrafts.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.PropertyDrafts.Data.Entities;
using Heimatplatz.Api.Features.PropertyDrafts.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.PropertyDrafts.Handlers;

/// <summary>
/// Loescht einen Entwurf samt der von ihm hochgeladenen Medien-Dateien
/// (uploads/listings) - sonst blieben sie fuer immer oeffentlich abrufbar liegen (DSGVO).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/property-drafts")]
public class DeletePropertyDraftHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IPropertyImageService imageService
) : IRequestHandler<DeletePropertyDraftRequest, DeletePropertyDraftResponse>
{
    [MediatorHttpDelete("/{Id}", OperationId = "DeletePropertyDraft", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireSeller])]
    public async Task<DeletePropertyDraftResponse> Handle(
        DeletePropertyDraftRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.GetRequiredUserId();

        var draft = await dbContext.Set<PropertyDraft>()
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Entwurf mit ID {request.Id} nicht gefunden.");

        if (draft.UserId != userId)
            throw new UnauthorizedAccessException("Sie haben keine Berechtigung fuer diesen Entwurf.");

        var data = DraftPayloadSerializer.Deserialize(draft.PayloadJson);

        dbContext.Set<PropertyDraft>().Remove(draft);
        await dbContext.SaveChangesAsync(cancellationToken);

        // DeleteImageAsync deckt alle lokalen uploads/*-Pfade ab (auch uploads/listings)
        // und ignoriert fremde Hosts selbst.
        var mediaUrls = (data.ImageUrls ?? []).Concat(data.VideoUrls ?? []);
        foreach (var url in mediaUrls)
        {
            await imageService.DeleteImageAsync(url, cancellationToken);
        }

        return new DeletePropertyDraftResponse(Success: true);
    }
}
