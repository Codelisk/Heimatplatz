using System.Text.Json;
using System.Text.Json.Serialization;
using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.AiListing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.AiListing.Contracts.Models;
using Heimatplatz.Api.Features.AiListing.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.AiListing.Handlers;

/// <summary>
/// Liefert Status und Ergebnis einer KI-Analyse (Polling-Endpoint).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/ai-listings")]
public class GetListingAnalysisHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GetListingAnalysisRequest, GetListingAnalysisResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [MediatorHttpGet("/{AnalysisId}", OperationId = "GetListingAnalysis", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireSeller])]
    public async Task<GetListingAnalysisResponse> Handle(
        GetListingAnalysisRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var analysis = await dbContext.Set<ListingAnalysis>()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AnalysisId && a.UserId == userId, cancellationToken)
            ?? throw new ArgumentException("Analyse nicht gefunden.");

        ExtractedListingData? result = null;
        if (analysis.ResultJson is not null)
            result = JsonSerializer.Deserialize<ExtractedListingData>(analysis.ResultJson, JsonOptions);

        return new GetListingAnalysisResponse(
            analysis.Id,
            analysis.Status,
            result,
            analysis.ErrorMessage,
            analysis.CreatedAt,
            analysis.CompletedAt
        );
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
