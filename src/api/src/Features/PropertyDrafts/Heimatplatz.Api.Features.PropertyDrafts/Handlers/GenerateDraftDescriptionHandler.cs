using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.PropertyDrafts.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.PropertyDrafts.Contracts.Models;
using Heimatplatz.Api.Features.PropertyDrafts.Data.Entities;
using Heimatplatz.Api.Features.PropertyDrafts.Infrastructure;
using Heimatplatz.Api.Features.PropertyDrafts.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.PropertyDrafts.Handlers;

/// <summary>
/// Fordert die asynchrone KI-Beschreibung fuer einen Entwurf an: setzt den Status auf
/// Queued und plant den TickerQ-Hintergrund-Job ein. Der Client pollt anschliessend
/// GetDraftDescription und kann sofort im Wizard weiterarbeiten.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/property-drafts")]
public class GenerateDraftDescriptionHandler(
    AppDbContext dbContext,
    IDraftDescriptionJobScheduler jobScheduler,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GenerateDraftDescriptionRequest, GenerateDraftDescriptionResponse>
{
    const int MinKeywordsLength = 3;
    const int MaxKeywordsLength = 2000;

    [MediatorHttpPost("/generate-description", OperationId = "GenerateDraftDescription", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireSeller])]
    public async Task<GenerateDraftDescriptionResponse> Handle(
        GenerateDraftDescriptionRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.GetRequiredUserId();

        var keywords = request.Keywords.Trim();
        if (keywords.Length < MinKeywordsLength)
            throw new ArgumentException("Bitte ein paar Stichwoerter zur Immobilie angeben.");
        if (keywords.Length > MaxKeywordsLength)
            throw new ArgumentException($"Die Stichwoerter sind zu lang (maximal {MaxKeywordsLength} Zeichen).");

        var draft = await dbContext.Set<PropertyDraft>()
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Entwurf mit ID {request.Id} nicht gefunden.");

        if (draft.UserId != userId)
            throw new UnauthorizedAccessException("Sie haben keine Berechtigung fuer diesen Entwurf.");

        if (draft.DescriptionStatus is DraftDescriptionStatus.Queued or DraftDescriptionStatus.InProgress)
            throw new ArgumentException("Es wird bereits eine Beschreibung fuer diesen Entwurf erstellt.");

        // Status VOR dem Einplanen persistieren, damit der Job den Entwurf nie im alten Zustand sieht
        draft.DescriptionStatus = DraftDescriptionStatus.Queued;
        draft.DescriptionKeywords = keywords;
        draft.GeneratedDescription = null;
        draft.DescriptionError = null;
        draft.DescriptionRequestedAt = DateTimeOffset.UtcNow;
        draft.DescriptionCompletedAt = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        await jobScheduler.ScheduleAsync(draft.Id, cancellationToken);

        return new GenerateDraftDescriptionResponse(draft.DescriptionStatus);
    }
}
