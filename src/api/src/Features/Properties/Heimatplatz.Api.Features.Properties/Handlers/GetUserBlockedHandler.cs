using System.IdentityModel.Tokens.Jwt;
using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Handler for GetUserBlockedRequest - returns all blocked properties for the authenticated user
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/blocked")]
public class GetUserBlockedHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GetUserBlockedRequest, GetUserBlockedResponse>
{
    [MediatorHttpGet("", OperationId = "GetUserBlocked", AuthorizationPolicies = [AuthorizationPolicies.RequireBuyer])]
    public async Task<GetUserBlockedResponse> Handle(GetUserBlockedRequest request, IMediatorContext context, CancellationToken cancellationToken)
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

        // Query blocked properties
        var properties = await dbContext.Set<Blocked>()
            .Where(b => b.UserId == userId)
            .Include(b => b.Property)
            .Select(b => new PropertyListItemDto(
                b.Property.Id,
                b.Property.Title,
                b.Property.Address,
                b.Property.City,
                b.Property.Price,
                b.Property.LivingAreaSquareMeters,
                b.Property.PlotAreaSquareMeters,
                b.Property.Rooms,
                b.Property.Type,
                b.Property.SellerType,
                b.Property.SellerName,
                b.Property.ImageUrls,
                b.Property.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new GetUserBlockedResponse(properties);
    }
}
