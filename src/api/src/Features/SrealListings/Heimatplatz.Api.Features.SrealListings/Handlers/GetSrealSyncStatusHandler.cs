using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.SrealListings.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.SrealListings.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.SrealListings.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/sreal-listings")]
public class GetSrealSyncStatusHandler(AppDbContext dbContext)
    : IRequestHandler<GetSrealSyncStatusRequest, GetSrealSyncStatusResponse>
{
    [MediatorHttpGet("/sync/status", OperationId = "GetSrealSyncStatus")]
    public async Task<GetSrealSyncStatusResponse> Handle(
        GetSrealSyncStatusRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var listings = dbContext.Set<SrealListing>();
        var changes = dbContext.Set<SrealListingChange>();

        var allListings = await listings.ToListAsync(cancellationToken);

        var lastSyncAt = allListings
            .Where(s => s.LastScrapedAt.HasValue)
            .Select(s => s.LastScrapedAt!.Value)
            .DefaultIfEmpty()
            .Max();

        return new GetSrealSyncStatusResponse
        {
            LastSyncAt = lastSyncAt == default ? null : lastSyncAt,
            TotalActiveListings = allListings.Count(s => s.IsActive),
            TotalRemovedListings = allListings.Count(s => !s.IsActive),
            TotalChanges = await changes.CountAsync(cancellationToken)
        };
    }
}
