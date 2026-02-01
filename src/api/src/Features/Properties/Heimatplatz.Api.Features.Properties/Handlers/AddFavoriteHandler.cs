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
/// Handler for AddFavoriteRequest - adds a property to the user's favorites
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/favorites")]
public class AddFavoriteHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<AddFavoriteRequest, AddFavoriteResponse>
{
    [MediatorHttpPost("", OperationId = "AddFavorite", AuthorizationPolicies = [AuthorizationPolicies.RequireAnyRole])]
    public async Task<AddFavoriteResponse> Handle(AddFavoriteRequest request, IMediatorContext context, CancellationToken cancellationToken)
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
            return new AddFavoriteResponse(false, "Immobilie nicht gefunden");
        }

        // Check if property is blocked (cannot favorite blocked properties - either-or relationship)
        var isBlocked = await dbContext.Set<Blocked>()
            .AnyAsync(b => b.UserId == userId && b.PropertyId == request.PropertyId, cancellationToken);

        if (isBlocked)
        {
            return new AddFavoriteResponse(false, "Blockierte Immobilie kann nicht favorisiert werden. Bitte zuerst Blockierung aufheben.");
        }

        // Check if favorite already exists
        var favoriteExists = await dbContext.Set<Favorite>()
            .AnyAsync(f => f.UserId == userId && f.PropertyId == request.PropertyId, cancellationToken);

        if (favoriteExists)
        {
            return new AddFavoriteResponse(false, "Immobilie ist bereits in Favoriten");
        }

        // Add favorite
        var favorite = new Favorite
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PropertyId = request.PropertyId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Set<Favorite>().Add(favorite);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AddFavoriteResponse(true, "Immobilie zu Favoriten hinzugefuegt");
    }
}
