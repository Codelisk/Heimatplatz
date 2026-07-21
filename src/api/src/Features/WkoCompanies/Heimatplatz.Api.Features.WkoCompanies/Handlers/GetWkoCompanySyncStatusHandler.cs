using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.WkoCompanies.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.WkoCompanies.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.WkoCompanies.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/wko-companies")]
public class GetWkoCompanySyncStatusHandler(AppDbContext dbContext)
    : IRequestHandler<GetWkoCompanySyncStatusRequest, GetWkoCompanySyncStatusResponse>
{
    [MediatorHttpGet("/sync/status", OperationId = "GetWkoCompanySyncStatus")]
    public async Task<GetWkoCompanySyncStatusResponse> Handle(
        GetWkoCompanySyncStatusRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var companies = dbContext.Set<WkoCompany>();

        var lastSyncAt = await companies
            .Where(c => c.LastScrapedAt.HasValue)
            .MaxAsync(c => c.LastScrapedAt, cancellationToken);

        return new GetWkoCompanySyncStatusResponse
        {
            LastSyncAt = lastSyncAt,
            TotalActiveCompanies = await companies.CountAsync(c => c.IsActive, cancellationToken),
            TotalRemovedCompanies = await companies.CountAsync(c => !c.IsActive, cancellationToken)
        };
    }
}
