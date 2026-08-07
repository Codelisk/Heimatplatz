using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Dashboards.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Dashboards.Data.Entities;
using Heimatplatz.Api.Features.Dashboards.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Handlers;

/// <summary>
/// Listet die Uebersichten des angemeldeten Nutzers (neueste zuerst).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/dashboards")]
public class GetDashboardsHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GetDashboardsRequest, GetDashboardsResponse>
{
    [MediatorHttpGet("/", OperationId = "GetDashboards", RequiresAuthorization = true)]
    public async Task<GetDashboardsResponse> Handle(
        GetDashboardsRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.GetRequiredUserId();

        var dashboards = await dbContext.Set<UserDashboard>()
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DashboardSummaryDto(d.Id, d.Title, d.GenerationStatus, d.CreatedAt, d.UpdatedAt, d.ViewType))
            .ToListAsync(cancellationToken);

        return new GetDashboardsResponse(dashboards);
    }
}
