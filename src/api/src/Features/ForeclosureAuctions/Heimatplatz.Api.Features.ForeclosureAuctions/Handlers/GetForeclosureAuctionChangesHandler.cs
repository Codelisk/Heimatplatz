using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.ForeclosureAuctions.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/foreclosure-auctions")]
public class GetForeclosureAuctionChangesHandler(AppDbContext dbContext)
    : IRequestHandler<GetForeclosureAuctionChangesRequest, GetForeclosureAuctionChangesResponse>
{
    // Obergrenze pro Seite: schuetzt vor PageSize=1000000-Anfragen
    private const int MaxPageSize = 200;

    [MediatorHttpGet("/changes", OperationId = "GetForeclosureAuctionChanges")]
    public async Task<GetForeclosureAuctionChangesResponse> Handle(
        GetForeclosureAuctionChangesRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<ForeclosureAuctionChange>().AsQueryable();

        // DateTimeOffset-Vergleiche laufen auch auf SQLite in SQL - die Konverter im
        // AppDbContext speichern dort als long (UTC-Ticks), kein In-Memory-Umweg noetig
        if (request.Since.HasValue)
        {
            var since = request.Since.Value.ToUniversalTime();
            query = query.Where(c => c.CreatedAt >= since);
        }

        if (!string.IsNullOrWhiteSpace(request.ChangeType))
            query = query.Where(c => c.ChangeType == request.ChangeType);

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var totalCount = await query.CountAsync(cancellationToken);

        // Sortierung + Paging in der Datenbank (SQLite-DateTimeOffset-ORDER-BY laeuft
        // ueber die Konverter im AppDbContext)
        var changes = await query
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ThenBy(c => c.Id) // stabiler Tiebreaker fuer deterministisches Paging
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ForeclosureAuctionChangeDto
            {
                Id = c.Id,
                ForeclosureAuctionId = c.ForeclosureAuctionId,
                ChangeType = c.ChangeType,
                ChangedFields = c.ChangedFields,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new GetForeclosureAuctionChangesResponse
        {
            Changes = changes,
            TotalCount = totalCount
        };
    }
}
