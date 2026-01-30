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
/// Handler for GetUserPropertiesRequest - returns all properties belonging to the authenticated user
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/properties")]
public class GetUserPropertiesHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GetUserPropertiesRequest, GetUserPropertiesResponse>
{
    [MediatorHttpGet("/user", OperationId = "GetUserProperties", AuthorizationPolicies = [AuthorizationPolicies.RequireSeller])]
    public async Task<GetUserPropertiesResponse> Handle(GetUserPropertiesRequest request, IMediatorContext context, CancellationToken cancellationToken)
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

        // Base query for user's properties
        var query = dbContext.Set<Property>()
            .Include(p => p.Municipality)
            .Where(p => p.UserId == userId);

        // Get total count
        var total = await query.CountAsync(cancellationToken);

        // Apply pagination and project to DTO
        var properties = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PropertyListItemDto(
                p.Id,
                p.Title,
                p.Address,
                p.MunicipalityId,
                p.Municipality.Name,
                p.Municipality.PostalCode,
                p.Price,
                p.LivingAreaSquareMeters,
                p.PlotAreaSquareMeters,
                p.Rooms,
                p.Type,
                p.SellerType,
                p.SellerName,
                p.ImageUrls,
                p.CreatedAt,
                p.InquiryType
            ))
            .ToListAsync(cancellationToken);

        var hasMore = (request.Page + 1) * request.PageSize < total;

        return new GetUserPropertiesResponse(
            properties,
            total,
            request.PageSize,
            request.Page,
            hasMore
        );
    }
}
