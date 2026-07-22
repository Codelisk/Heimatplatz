using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Firmenbuch.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Firmenbuch.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Firmenbuch.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/firmenbuch")]
public class GetFirmenbuchCatalogStatusHandler(AppDbContext dbContext)
    : IRequestHandler<GetFirmenbuchCatalogStatusRequest, GetFirmenbuchCatalogStatusResponse>
{
    [MediatorHttpGet("/catalog/status", OperationId = "GetFirmenbuchCatalogStatus")]
    public async Task<GetFirmenbuchCatalogStatusResponse> Handle(
        GetFirmenbuchCatalogStatusRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var companies = dbContext.Set<FirmenbuchCompany>();

        var total = await companies.CountAsync(cancellationToken);

        var byStatusRaw = await companies
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        DateTimeOffset? lastSyncAt = total > 0
            ? await companies.MaxAsync(c => c.LastSeenAt, cancellationToken)
            : null;

        return new GetFirmenbuchCatalogStatusResponse
        {
            TotalCompanies = total,
            ActiveCompanies = byStatusRaw.FirstOrDefault(s => s.Status == null)?.Count ?? 0,
            ByStatus = byStatusRaw
                .Where(s => s.Status != null)
                .ToDictionary(s => s.Status!, s => s.Count),
            LastSyncAt = lastSyncAt
        };
    }
}
