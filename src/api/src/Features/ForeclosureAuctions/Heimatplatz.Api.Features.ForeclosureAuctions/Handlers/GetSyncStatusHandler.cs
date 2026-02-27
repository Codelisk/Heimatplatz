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
public class GetSyncStatusHandler(AppDbContext dbContext)
    : IRequestHandler<GetSyncStatusRequest, GetSyncStatusResponse>
{
    [MediatorHttpGet("/sync/status", OperationId = "GetSyncStatus")]
    public async Task<GetSyncStatusResponse> Handle(
        GetSyncStatusRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var auctions = dbContext.Set<ForeclosureAuction>();
        var changes = dbContext.Set<ForeclosureAuctionChange>();

        var allAuctions = await auctions.ToListAsync(cancellationToken);

        var lastSyncAt = allAuctions
            .Where(a => a.LastScrapedAt.HasValue)
            .Select(a => a.LastScrapedAt!.Value)
            .DefaultIfEmpty()
            .Max();

        return new GetSyncStatusResponse
        {
            LastSyncAt = lastSyncAt == default ? null : lastSyncAt,
            TotalActiveAuctions = allAuctions.Count(a => a.IsActive),
            TotalRemovedAuctions = allAuctions.Count(a => !a.IsActive),
            TotalChanges = await changes.CountAsync(cancellationToken)
        };
    }
}
