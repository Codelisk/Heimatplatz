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
/// Loescht eine Uebersicht samt aller Revisionen. Ein evtl. noch laufender
/// Generierungs-Job findet die Revision danach nicht mehr und beendet sich leise.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/dashboards")]
public class DeleteDashboardHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<DeleteDashboardRequest, DeleteDashboardResponse>
{
    [MediatorHttpDelete("/{Id}", OperationId = "DeleteDashboard", RequiresAuthorization = true)]
    public async Task<DeleteDashboardResponse> Handle(
        DeleteDashboardRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.GetRequiredUserId();

        var dashboard = await dbContext.Set<UserDashboard>()
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Übersicht mit ID {request.Id} nicht gefunden.");

        if (dashboard.UserId != userId)
            throw new UnauthorizedAccessException("Sie haben keine Berechtigung für diese Übersicht.");

        // Revisionen explizit zuerst (ExecuteDelete verlaesst sich nicht auf FK-Kaskaden)
        await dbContext.Set<UserDashboardRevision>()
            .Where(r => r.DashboardId == dashboard.Id)
            .ExecuteDeleteAsync(cancellationToken);

        dbContext.Set<UserDashboard>().Remove(dashboard);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteDashboardResponse(true);
    }
}
