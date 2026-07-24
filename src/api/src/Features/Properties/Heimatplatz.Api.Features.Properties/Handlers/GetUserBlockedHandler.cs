using System.IdentityModel.Tokens.Jwt;
using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Handler for GetUserBlockedRequest - returns all blocked properties for the authenticated user
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/blocked")]
public class GetUserBlockedHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration
) : IRequestHandler<GetUserBlockedRequest, GetUserBlockedResponse>
{
    [MediatorHttpGet("", OperationId = "GetUserBlocked", RequiresAuthorization = true)]
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

        // Base query for blocked properties (admin-seitig ausgeblendete Inserate bleiben draussen -
        // sonst sieht ein Nutzer, der ein spaeter moderiertes Inserat blockiert hatte, dessen
        // vollen Inhalt weiterhin ueber die Blockiert-Liste)
        var query = dbContext.Set<Blocked>()
            .Where(b => b.UserId == userId && !b.Property.IsHidden)
            .Include(b => b.Property)
                .ThenInclude(p => p.Municipality);

        // Get total count
        var total = await query.CountAsync(cancellationToken);

        // Seite laden und in-memory projizieren (AuctionDate steckt im TypeSpecificData-JSON
        // und ist nicht in SQL uebersetzbar). Bild-Proxy wie Favoriten/Suche - sonst blockt
        // die Web-CSP externe Bildquellen.
        var blockedProperties = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .Select(b => b.Property)
            .ToListAsync(cancellationToken);

        var baseUrl = GetPropertiesHandler.ResolveApiBaseUrl(httpContextAccessor, configuration);
        var properties = blockedProperties
            .Select(p => new PropertyListItemDto(
                p.Id,
                p.Title,
                p.Address,
                p.MunicipalityId,
                p.Municipality.Name,
                p.PostalCode ?? p.Municipality.PostalCode,
                p.Price,
                p.LivingAreaSquareMeters,
                p.PlotAreaSquareMeters,
                p.Rooms,
                p.Type,
                p.SellerType,
                p.SellerName,
                GetPropertiesHandler.ProxyImageUrls(p.ImageUrls, baseUrl, width: GetPropertiesHandler.ListThumbnailWidth),
                p.CreatedAt,
                p.InquiryType,
                p.SourceName,
                GetPropertiesHandler.ResolveAuctionDate(p)
            ))
            .ToList();

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
