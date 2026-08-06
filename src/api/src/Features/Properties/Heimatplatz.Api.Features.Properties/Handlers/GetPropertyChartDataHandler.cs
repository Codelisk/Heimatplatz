using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Heimatplatz.Api.Features.Properties.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Rohwerte (Preis + Einstelldatum) einer gefilterten Treffermenge fuer
/// server-gerenderte Diagramme. Nutzt dieselbe Filterlogik wie Trefferliste,
/// Karte und Kennzahlen (PropertyQueryFilters) - alle meinen dieselbe Menge.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/properties")]
public class GetPropertyChartDataHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GetPropertyChartDataRequest, GetPropertyChartDataResponse>
{
    [MediatorHttpGet("/chart-data", OperationId = "GetPropertyChartData")]
    public async Task<GetPropertyChartDataResponse> Handle(GetPropertyChartDataRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Property>()
            .AsNoTracking()
            .Where(p => !p.IsHidden);

        query = PropertyQueryFilters.ExcludeBlockedForCurrentUser(query, dbContext, httpContextAccessor);
        query = PropertyQueryFilters.ApplyCommonFilters(
            query,
            request.GetPropertyTypes(),
            request.GetSellerTypes(),
            request.GetMunicipalityIds(),
            request.CreatedAfter,
            request.PriceMin,
            request.PriceMax,
            request.AreaMin,
            request.AreaMax,
            request.RoomsMin,
            request.SearchText,
            request.GetExcludedSellerSourceIds(),
            request.IncludeNewBuildProjects);

        var total = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Take(GetPropertyChartDataRequest.MaxItems)
            .Select(p => new { p.Price, p.CreatedAt })
            .ToListAsync(cancellationToken);

        return new GetPropertyChartDataResponse(
            total,
            rows.Select(r => r.Price).ToList(),
            rows.Select(r => r.CreatedAt.UtcDateTime).ToList(),
            Truncated: total > rows.Count);
    }
}
