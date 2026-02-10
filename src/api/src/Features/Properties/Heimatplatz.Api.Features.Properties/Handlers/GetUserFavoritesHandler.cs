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
/// Handler for GetUserFavoritesRequest - returns all favorited properties for the authenticated user
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/favorites")]
public class GetUserFavoritesHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GetUserFavoritesRequest, GetUserFavoritesResponse>
{
    [MediatorHttpGet("", OperationId = "GetUserFavorites", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireAnyRole])]
    public async Task<GetUserFavoritesResponse> Handle(GetUserFavoritesRequest request, IMediatorContext context, CancellationToken cancellationToken)
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

        // Base query for favorited properties
        var query = dbContext.Set<Favorite>()
            .Where(f => f.UserId == userId)
            .Include(f => f.Property)
                .ThenInclude(p => p.Municipality);

        // Get total count
        var total = await query.CountAsync(cancellationToken);

        // Apply pagination and project to DTO
        var properties = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .Select(f => new PropertyListItemDto(
                f.Property.Id,
                f.Property.Title,
                f.Property.Address,
                f.Property.MunicipalityId,
                f.Property.Municipality.Name,
                f.Property.Municipality.PostalCode,
                f.Property.Price,
                f.Property.LivingAreaSquareMeters,
                f.Property.PlotAreaSquareMeters,
                f.Property.Rooms,
                f.Property.Type,
                f.Property.SellerType,
                f.Property.SellerName,
                f.Property.ImageUrls,
                f.Property.CreatedAt,
                f.Property.InquiryType
            ))
            .ToListAsync(cancellationToken);

        var hasMore = (request.Page + 1) * request.PageSize < total;

        return new GetUserFavoritesResponse(
            properties,
            total,
            request.PageSize,
            request.Page,
            hasMore
        );
    }
}
