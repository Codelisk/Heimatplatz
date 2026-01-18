using System.IdentityModel.Tokens.Jwt;
using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Handler for RemoveFavoriteRequest - removes a property from the user's favorites
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/favorites")]
public class RemoveFavoriteHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<RemoveFavoriteRequest, RemoveFavoriteResponse>
{
    [MediatorHttpDelete("/{PropertyId}", OperationId = "RemoveFavorite", AuthorizationPolicies = [AuthorizationPolicies.RequireBuyer])]
    public async Task<RemoveFavoriteResponse> Handle(RemoveFavoriteRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        // Extract UserId from JWT Token
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext ist nicht verfuegbar");

        var userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Benutzer-ID nicht im Token gefunden");

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Ungueltige Benutzer-ID im Token");
        }

        // Find the favorite
        var favorite = await dbContext.Set<Favorite>()
            .FirstOrDefaultAsync(f => f.UserId == userId && f.PropertyId == request.PropertyId, cancellationToken);

        if (favorite == null)
        {
            return new RemoveFavoriteResponse(false, "Favorit nicht gefunden");
        }

        // Remove favorite
        dbContext.Set<Favorite>().Remove(favorite);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RemoveFavoriteResponse(true, "Immobilie aus Favoriten entfernt");
    }
}
