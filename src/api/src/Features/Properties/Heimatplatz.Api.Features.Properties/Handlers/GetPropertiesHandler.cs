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
/// Handler fuer GetPropertiesRequest - gibt gefilterte Immobilien-Liste zurueck
/// Blockierte Properties werden fuer authentifizierte Benutzer automatisch ausgeblendet
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

        // Filter anwenden
        if (request.Typ.HasValue)
            query = query.Where(p => p.Type == request.Typ.Value);

        if (request.AnbieterTyp.HasValue)
            query = query.Where(p => p.SellerType == request.AnbieterTyp.Value);

        if (request.PreisMin.HasValue)
            query = query.Where(p => p.Price >= request.PreisMin.Value);

        if (request.PreisMax.HasValue)
            query = query.Where(p => p.Price <= request.PreisMax.Value);

        if (request.FlaecheMin.HasValue)
            query = query.Where(p => (p.LivingAreaSquareMeters ?? p.PlotAreaSquareMeters) >= request.FlaecheMin.Value);

        if (request.FlaecheMax.HasValue)
            query = query.Where(p => (p.LivingAreaSquareMeters ?? p.PlotAreaSquareMeters) <= request.FlaecheMax.Value);

        if (request.ZimmerMin.HasValue)
            query = query.Where(p => p.Rooms >= request.ZimmerMin.Value);

        if (!string.IsNullOrWhiteSpace(request.Ort))
            query = query.Where(p => p.City.Contains(request.Ort) || p.PostalCode.StartsWith(request.Ort));

        // Gesamtanzahl fuer Paging
        var gesamt = await query.CountAsync(cancellationToken);

        // Laden, Sortierung im Speicher (SQLite unterstuetzt DateTimeOffset nicht in ORDER BY), dann Paging
        var entities = await query.ToListAsync(cancellationToken);
        var immobilien = entities
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

        return new GetPropertiesResponse(immobilien, gesamt);
    }
}
