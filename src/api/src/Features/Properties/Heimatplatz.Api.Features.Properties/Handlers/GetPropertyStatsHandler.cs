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
/// Kennzahlen zu einer gefilterten Treffermenge (Total, Neu in 7 Tagen,
/// Min-/Median-/Max-Preis). Nutzt dieselbe Filterlogik wie Trefferliste und
/// Karte (PropertyQueryFilters), damit alle drei dieselbe Menge meinen.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/properties")]
public class GetPropertyStatsHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GetPropertyStatsRequest, GetPropertyStatsResponse>
{
    [MediatorHttpGet("/stats", OperationId = "GetPropertyStats")]
    public async Task<GetPropertyStatsResponse> Handle(GetPropertyStatsRequest request, IMediatorContext context, CancellationToken cancellationToken)
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
        if (total == 0)
            return new GetPropertyStatsResponse(0, 0, null, null, null);

        var sevenDaysAgo = DateTimeOffset.UtcNow.AddDays(-7);
        var newLast7Days = await query.CountAsync(p => p.CreatedAt >= sevenDaysAgo, cancellationToken);

        var minPrice = await query.MinAsync(p => (decimal?)p.Price, cancellationToken);
        var maxPrice = await query.MaxAsync(p => (decimal?)p.Price, cancellationToken);

        // Median als geordneter Skip statt In-Memory-Laden - uebersetzt auf allen Providern
        // (SQLite-decimal-ORDER-BY laeuft ueber die Konverter im AppDbContext).
        var medianPrice = await query
            .OrderBy(p => p.Price)
            .Select(p => (decimal?)p.Price)
            .Skip((total - 1) / 2)
            .FirstOrDefaultAsync(cancellationToken);

        return new GetPropertyStatsResponse(total, newLast7Days, minPrice, medianPrice, maxPrice);
    }
}
