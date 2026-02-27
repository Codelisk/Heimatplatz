using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.ForeclosureAuctions.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
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

        if (request.Since.HasValue)
            query = query.Where(c => c.CreatedAt >= request.Since.Value);

        if (!string.IsNullOrWhiteSpace(request.ChangeType))
            query = query.Where(c => c.ChangeType == request.ChangeType);

        var totalCount = await query.CountAsync(cancellationToken);

        // SQLite DateTimeOffset ORDER BY workaround
        var entities = await query.ToListAsync(cancellationToken);
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
