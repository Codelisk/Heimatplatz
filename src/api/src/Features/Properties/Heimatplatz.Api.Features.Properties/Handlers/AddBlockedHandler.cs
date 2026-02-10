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
/// Handler for AddBlockedRequest - adds a property to the user's blocked list.
/// If the property is favorited, it will be removed from favorites (either-or relationship).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/blocked")]
public class AddBlockedHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<AddBlockedRequest, AddBlockedResponse>
{
    [MediatorHttpPost("", OperationId = "AddBlocked", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireBuyer])]
    public async Task<AddBlockedResponse> Handle(AddBlockedRequest request, IMediatorContext context, CancellationToken cancellationToken)
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

        // Check if property exists
        var propertyExists = await dbContext.Set<Property>()
            .AnyAsync(p => p.Id == request.PropertyId, cancellationToken);

        if (!propertyExists)
        {
            return new AddBlockedResponse(false, "Immobilie nicht gefunden");
        }

        // Check if already blocked
        var blockedExists = await dbContext.Set<Blocked>()
            .AnyAsync(b => b.UserId == userId && b.PropertyId == request.PropertyId, cancellationToken);

        if (blockedExists)
        {
            return new AddBlockedResponse(false, "Immobilie ist bereits blockiert");
        }

        // Check if property is favorited and remove from favorites (either-or relationship)
        var wasRemovedFromFavorites = false;
        var favorite = await dbContext.Set<Favorite>()
            .FirstOrDefaultAsync(f => f.UserId == userId && f.PropertyId == request.PropertyId, cancellationToken);

        if (favorite != null)
        {
            dbContext.Set<Favorite>().Remove(favorite);
            wasRemovedFromFavorites = true;
        }

        // Add to blocked list
        var blocked = new Blocked
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PropertyId = request.PropertyId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Set<Blocked>().Add(blocked);
        await dbContext.SaveChangesAsync(cancellationToken);

        var message = wasRemovedFromFavorites
            ? "Immobilie blockiert und aus Favoriten entfernt"
            : "Immobilie blockiert";

        return new AddBlockedResponse(true, message, wasRemovedFromFavorites);
    }
}
