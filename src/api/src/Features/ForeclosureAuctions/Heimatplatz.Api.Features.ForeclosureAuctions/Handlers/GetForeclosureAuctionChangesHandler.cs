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
    [MediatorHttpGet("/changes", OperationId = "GetForeclosureAuctionChanges")]
    public async Task<GetForeclosureAuctionChangesResponse> Handle(
        GetForeclosureAuctionChangesRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<ForeclosureAuctionChange>().AsQueryable();

        // SQLite kann DateTimeOffset-Vergleiche nicht in SQL uebersetzen -> dort nach dem Laden filtern
        var isSqlite = dbContext.Database.IsSqlite();
        if (request.Since.HasValue && !isSqlite)
            query = query.Where(c => c.CreatedAt >= request.Since.Value);

        if (!string.IsNullOrWhiteSpace(request.ChangeType))
            query = query.Where(c => c.ChangeType == request.ChangeType);

        // SQLite DateTimeOffset ORDER BY workaround
        var entities = await query.ToListAsync(cancellationToken);
        if (request.Since.HasValue && isSqlite)
            entities = entities.Where(c => c.CreatedAt >= request.Since.Value).ToList();

        // Query enthaelt keine Pagination -> Gesamtzahl entspricht den geladenen Eintraegen
        var totalCount = entities.Count;
        var changes = entities
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new ForeclosureAuctionChangeDto
            {
                Id = c.Id,
                ForeclosureAuctionId = c.ForeclosureAuctionId,
                ChangeType = c.ChangeType,
                ChangedFields = c.ChangedFields,
                CreatedAt = c.CreatedAt
            })
            .ToList();

        return new GetForeclosureAuctionChangesResponse
        {
            Changes = changes,
            TotalCount = totalCount
        };
    }
}
