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
        // Include Municipality for City/PostalCode values
        var query = dbContext.Set<Property>()
            .Include(p => p.Municipality)
            .AsQueryable();

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

        // Filter: PropertyTypes (Multi-Select)
        var propertyTypes = request.GetPropertyTypes();
        if (propertyTypes.Count > 0)
            query = query.Where(p => propertyTypes.Contains(p.Type));

        // Filter: SellerTypes (Multi-Select)
        var sellerTypes = request.GetSellerTypes();
        if (sellerTypes.Count > 0)
            query = query.Where(p => sellerTypes.Contains(p.SellerType));

        // Filter: MunicipalityIds (Multi-Select)
        var municipalityIds = request.GetMunicipalityIds();
        if (municipalityIds.Count > 0)
            query = query.Where(p => municipalityIds.Contains(p.MunicipalityId));

        // Filter: CreatedAfter (Age filter) - convert to UTC DateTimeOffset for proper comparison
        if (request.CreatedAfter.HasValue)
        {
            var createdAfterUtc = new DateTimeOffset(request.CreatedAfter.Value.ToUniversalTime(), TimeSpan.Zero);
            query = query.Where(p => p.CreatedAt >= createdAfterUtc);
        }

        // Filter: Price range
        if (request.PriceMin.HasValue)
            query = query.Where(p => p.Price >= request.PriceMin.Value);

        if (request.PriceMax.HasValue)
            query = query.Where(p => p.Price <= request.PriceMax.Value);

        // Filter: Area range (use LivingArea if available, otherwise PlotArea)
        if (request.AreaMin.HasValue)
            query = query.Where(p => (p.LivingAreaSquareMeters ?? p.PlotAreaSquareMeters) >= request.AreaMin.Value);

        if (request.AreaMax.HasValue)
            query = query.Where(p => (p.LivingAreaSquareMeters ?? p.PlotAreaSquareMeters) <= request.AreaMax.Value);

        // Filter: Minimum rooms
        if (request.RoomsMin.HasValue)
            query = query.Where(p => p.Rooms >= request.RoomsMin.Value);

        // Filter: Exclude specific seller sources by name lookup
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

        // Total count for paging (before applying pagination)
        var total = await query.CountAsync(cancellationToken);

        // Calculate pagination
        var skip = request.Page * request.PageSize;
        var hasMore = (request.Page + 1) * request.PageSize < total;

        // Load, sort in memory (SQLite does not support DateTimeOffset in ORDER BY), then page
        var entities = await query.ToListAsync(cancellationToken);
        var properties = entities
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
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
            .ToList();

        return new GetPropertiesResponse(
            properties,
            total,
            request.PageSize,
            request.Page,
            hasMore
        );
    }
}
