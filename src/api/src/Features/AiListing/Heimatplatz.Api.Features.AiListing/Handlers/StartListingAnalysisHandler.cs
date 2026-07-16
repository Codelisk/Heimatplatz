using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.AiListing.Configuration;
using Heimatplatz.Api.Features.AiListing.Contracts.Enums;
using Heimatplatz.Api.Features.AiListing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.AiListing.Data.Entities;
using Heimatplatz.Api.Features.AiListing.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.AiListing.Handlers;

/// <summary>
/// Startet eine asynchrone KI-Analyse: legt den Job an und reiht ihn in die Queue ein.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/ai-listings")]
public class StartListingAnalysisHandler(
    AppDbContext dbContext,
    ListingAnalysisQueue queue,
    IOptions<AiListingOptions> options,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<StartListingAnalysisRequest, StartListingAnalysisResponse>
{
    [MediatorHttpPost("/", OperationId = "StartListingAnalysis", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireSeller])]
    public async Task<StartListingAnalysisResponse> Handle(
        StartListingAnalysisRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var opts = options.Value;

        if (request.ImageUrls.Count == 0 && string.IsNullOrWhiteSpace(request.DictatedText))
            throw new ArgumentException("Mindestens ein Foto oder eine diktierte Beschreibung ist erforderlich.");

        // Der AiConnector-Provider bekommt keine Medien uebertragen - ohne Text wuerde die
        // Analyse erst im Worker scheitern; hier frueh und mit klarer Meldung ablehnen
        if (string.Equals(opts.Provider, "AiConnector", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(request.DictatedText)
            && string.IsNullOrWhiteSpace(request.UserNotes))
            throw new ArgumentException("Eine Beschreibung (Diktat oder Notizen) ist erforderlich - Fotos allein reichen fuer die KI-Analyse nicht aus.");

        if (request.ImageUrls.Count > opts.MaxImages)
            throw new ArgumentException($"Maximal {opts.MaxImages} Fotos erlaubt.");

        if ((request.VideoUrls?.Count ?? 0) > opts.MaxVideos)
            throw new ArgumentException($"Maximal {opts.MaxVideos} Videos erlaubt.");

        var userId = GetCurrentUserId();

        var analysis = new ListingAnalysis
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = ListingAnalysisStatus.Queued,
            ImageUrls = request.ImageUrls,
            VideoUrls = request.VideoUrls ?? [],
            DictatedText = request.DictatedText,
            UserNotes = request.UserNotes
        };

        dbContext.Set<ListingAnalysis>().Add(analysis);
        await dbContext.SaveChangesAsync(cancellationToken);

        await queue.EnqueueAsync(analysis.Id, cancellationToken);

        return new StartListingAnalysisResponse(analysis.Id, analysis.Status);
    }

    private Guid GetCurrentUserId()
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext ist nicht verfuegbar.");

        var userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Benutzer-ID nicht im Token gefunden.");

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
            throw new UnauthorizedAccessException("Ungueltige Benutzer-ID im Token.");

        return userId;
    }
}
