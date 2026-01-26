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
/// Handler for GetPropertiesRequest - returns a filtered list of properties.
/// Blocked properties are automatically excluded for authenticated users.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/properties")]
public class GetPropertiesHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GetPropertiesRequest, GetPropertiesResponse>
{
    [MediatorHttpGet("/", OperationId = "GetProperties", AuthorizationPolicies = [AuthorizationPolicies.RequireAnyRole])]
    public async Task<GetPropertiesResponse> Handle(GetPropertiesRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Property>().AsQueryable();

        // Exclude blocked properties for authenticated users
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                var blockedPropertyIds = dbContext.Set<Blocked>()
                    .Where(b => b.UserId == userId)
                    .Select(b => b.PropertyId);

                query = query.Where(p => !blockedPropertyIds.Contains(p.Id));
            }
        }

        // Apply filters
        if (request.Type.HasValue)
            query = query.Where(p => p.Type == request.Type.Value);

        var sellerTypes = request.GetSellerTypes();
        if (sellerTypes.Count > 0)
            query = query.Where(p => sellerTypes.Contains(p.SellerType));

        if (request.PriceMin.HasValue)
            query = query.Where(p => p.Price >= request.PriceMin.Value);

        if (request.PriceMax.HasValue)
            query = query.Where(p => p.Price <= request.PriceMax.Value);

        if (request.AreaMin.HasValue)
            query = query.Where(p => (p.LivingAreaSquareMeters ?? p.PlotAreaSquareMeters) >= request.AreaMin.Value);

        if (request.AreaMax.HasValue)
            query = query.Where(p => (p.LivingAreaSquareMeters ?? p.PlotAreaSquareMeters) <= request.AreaMax.Value);

        if (request.RoomsMin.HasValue)
            query = query.Where(p => p.Rooms >= request.RoomsMin.Value);

        if (!string.IsNullOrWhiteSpace(request.City))
            query = query.Where(p => p.City.Contains(request.City) || p.PostalCode.StartsWith(request.City));

        // Exclude specific seller sources by name lookup
        var excludedSourceIds = request.GetExcludedSellerSourceIds();
        if (excludedSourceIds.Count > 0)
        {
            var excludedNames = await dbContext.Set<SellerSource>()
                .Where(ss => excludedSourceIds.Contains(ss.Id))
                .Select(ss => ss.Name)
                .ToListAsync(cancellationToken);

            if (excludedNames.Count > 0)
                query = query.Where(p => !excludedNames.Contains(p.SellerName));
        }

        // Total count for paging
        var total = await query.CountAsync(cancellationToken);

        // Load, sort in memory (SQLite does not support DateTimeOffset in ORDER BY), then page
        var entities = await query.ToListAsync(cancellationToken);
        var properties = entities
            .OrderByDescending(p => p.CreatedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(p => new PropertyListItemDto(
                p.Id,
                p.Title,
                p.Address,
                p.City,
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
            .ToList();

        return new GetPropertiesResponse(properties, total);
    }
}
