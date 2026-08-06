using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Dashboards.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Heimatplatz.Api.Features.Dashboards.Data.Entities;
using Heimatplatz.Api.Features.Dashboards.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Handlers;

/// <summary>
/// Spielt die vorherige Fassung zurueck (kein KI-Aufruf): die juengste Revision mit
/// Definition wird verworfen, die davor wird wieder aktiv. Mehrfaches Zurueckspringen
/// geht schrittweise weiter in die Historie.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/dashboards")]
public class RevertDashboardHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<RevertDashboardRequest, RevertDashboardResponse>
{
    [MediatorHttpPost("/revert", OperationId = "RevertDashboard", RequiresAuthorization = true)]
    public async Task<RevertDashboardResponse> Handle(
        RevertDashboardRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.GetRequiredUserId();

        var dashboard = await dbContext.Set<UserDashboard>()
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Übersicht mit ID {request.Id} nicht gefunden.");

        if (dashboard.UserId != userId)
            throw new UnauthorizedAccessException("Sie haben keine Berechtigung für diese Übersicht.");

        if (dashboard.GenerationStatus is DashboardGenerationStatus.Queued or DashboardGenerationStatus.InProgress)
            throw new ArgumentException("Diese Übersicht wird gerade erstellt - bitte warten Sie den Abschluss ab.");

        var revisions = await dbContext.Set<UserDashboardRevision>()
            .Where(r => r.DashboardId == dashboard.Id && r.DefinitionJson != null)
            .OrderByDescending(r => r.CreatedAt)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (revisions.Count < 2)
            return new RevertDashboardResponse(false);

        var target = revisions[1];

        dashboard.DefinitionJson = target.DefinitionJson;
        dashboard.GenerationStatus = DashboardGenerationStatus.Finished;
        dashboard.GenerationError = null;

        var targetDefinition = DashboardDefinitionSerializer.DeserializeStored(target.DefinitionJson);
        if (targetDefinition is not null)
        {
            dashboard.Title = targetDefinition.Title;
            dashboard.SchemaVersion = targetDefinition.SchemaVersion;
        }

        // Alles nach der Ziel-Revision verwerfen (auch fehlgeschlagene Runden) -
        // erneutes Zurueckspringen landet dann bei der naechstaelteren Fassung
        await dbContext.Set<UserDashboardRevision>()
            .Where(r => r.DashboardId == dashboard.Id && r.CreatedAt > target.CreatedAt)
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RevertDashboardResponse(true);
    }
}
