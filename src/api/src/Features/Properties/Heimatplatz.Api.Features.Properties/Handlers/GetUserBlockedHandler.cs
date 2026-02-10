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
    [MediatorHttpGet("", OperationId = "GetUserBlocked", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireBuyer])]
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

        // Base query for blocked properties
        var query = dbContext.Set<Blocked>()
            .Where(b => b.UserId == userId)
            .Include(b => b.Property)
                .ThenInclude(p => p.Municipality);

        // Get total count
        var total = await query.CountAsync(cancellationToken);

        // Apply pagination and project to DTO
        var properties = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .Select(b => new PropertyListItemDto(
                b.Property.Id,
                b.Property.Title,
                b.Property.Address,
                b.Property.MunicipalityId,
                b.Property.Municipality.Name,
                b.Property.Municipality.PostalCode,
                b.Property.Price,
                b.Property.LivingAreaSquareMeters,
                b.Property.PlotAreaSquareMeters,
                b.Property.Rooms,
                b.Property.Type,
                b.Property.SellerType,
                b.Property.SellerName,
                b.Property.ImageUrls,
                b.Property.CreatedAt,
                b.Property.InquiryType
            ))
            .ToListAsync(cancellationToken);

        var hasMore = (request.Page + 1) * request.PageSize < total;

        return new GetUserBlockedResponse(
            properties,
            total,
            request.PageSize,
            request.Page,
            hasMore
        );
    }
}
