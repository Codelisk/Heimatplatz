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

        // Nur Aggregate abfragen statt die ganze Tabelle zu laden
        var lastSyncAt = await auctions
            .Where(a => a.LastScrapedAt.HasValue)
            .MaxAsync(a => a.LastScrapedAt, cancellationToken);

        return new GetSyncStatusResponse
        {
            LastSyncAt = lastSyncAt,
            TotalActiveAuctions = await auctions.CountAsync(a => a.IsActive, cancellationToken),
            TotalRemovedAuctions = await auctions.CountAsync(a => !a.IsActive, cancellationToken),
            TotalChanges = await changes.CountAsync(cancellationToken)
        };
    }
}
