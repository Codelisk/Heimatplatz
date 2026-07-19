using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Heimatplatz.Api.Features.Properties.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Admin.Handlers;

/// <summary>
/// Inseratsliste fuer den Intern-Bereich - im Gegensatz zu den oeffentlichen
/// Endpoints inklusive ausgeblendeter Inserate (IsHidden), mit Eigentuemer-E-Mail
/// und Quellen-Filter (Nutzer-Inserate vs. Zwangsversteigerungen).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin")]
public class GetAdminPropertiesHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration
) : IRequestHandler<GetAdminPropertiesRequest, GetAdminPropertiesResponse>
{
    private const int MaxPageSize = 200;

    public const string SourceUser = "user";
    public const string SourceForeclosure = "foreclosure";

    [MediatorHttpGet("/properties", OperationId = "GetAdminProperties")]
    public async Task<GetAdminPropertiesResponse> Handle(GetAdminPropertiesRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var query = dbContext.Set<Property>()
            .Include(p => p.Municipality)
            .Include(p => p.User)
            .AsNoTracking();

        // Quelle: Zwangsversteigerungen kommen ausschliesslich als Type=Foreclosure aus
        // dem Edikte-Sync, alles andere sind Nutzer-Inserate (inkl. Batch-Import)
        query = request.Source?.ToLowerInvariant() switch
        {
            SourceForeclosure => query.Where(p => p.Type == PropertyType.Foreclosure),
            SourceUser => query.Where(p => p.Type != PropertyType.Foreclosure),
            _ => query
        };

        if (request.Hidden.HasValue)
            query = query.Where(p => p.IsHidden == request.Hidden.Value);

        if (request.UserId.HasValue)
            query = query.Where(p => p.UserId == request.UserId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(search) ||
                p.Address.ToLower().Contains(search) ||
                p.Municipality.Name.ToLower().Contains(search) ||
                p.SellerName.ToLower().Contains(search) ||
                p.User.Email.ToLower().Contains(search));
        }

        var page = Math.Max(request.Page, 0);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var total = await query.CountAsync(cancellationToken);
        var hasMore = (page + 1) * pageSize < total;

        var entities = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var baseUrl = GetPropertiesHandler.ResolveApiBaseUrl(httpContextAccessor, configuration);

        var properties = entities
            .Select(p => new AdminPropertyDto(
                p.Id,
                p.Title,
                p.Address,
                p.Municipality.Name,
                p.Municipality.PostalCode,
                p.Price,
                p.Type,
                p.SellerType,
                p.SellerName,
                p.UserId,
                p.User.Email,
                p.SourceName,
                p.SourceUrl,
                p.IsHidden,
                p.CreatedAt,
                GetPropertiesHandler.ProxyImageUrls(p.ImageUrls, baseUrl, width: GetPropertiesHandler.ListThumbnailWidth)
                    .FirstOrDefault()
            ))
            .ToList();

        return new GetAdminPropertiesResponse(properties, total, pageSize, page, hasMore);
    }
}
