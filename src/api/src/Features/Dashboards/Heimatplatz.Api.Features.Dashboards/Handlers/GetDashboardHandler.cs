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
/// Status + validierte Definition einer Uebersicht; zugleich der Polling-Endpoint
/// waehrend der KI-Generierung. Waehrend einer Verfeinerungsrunde bleibt die
/// bisherige Definition in der Antwort (das UI zeigt sie mit Hinweis weiter an).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/dashboards")]
public class GetDashboardHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GetDashboardRequest, GetDashboardResponse>
{
    [MediatorHttpGet("/{Id}", OperationId = "GetDashboard", RequiresAuthorization = true)]
    public async Task<GetDashboardResponse> Handle(
        GetDashboardRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.GetRequiredUserId();

        var dashboard = await dbContext.Set<UserDashboard>()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Übersicht mit ID {request.Id} nicht gefunden.");

        if (dashboard.UserId != userId)
            throw new UnauthorizedAccessException("Sie haben keine Berechtigung für diese Übersicht.");

        var definition = DashboardDefinitionSerializer.DeserializeStored(dashboard.DefinitionJson);

        var revisionsWithDefinition = await dbContext.Set<UserDashboardRevision>()
            .AsNoTracking()
            .CountAsync(r => r.DashboardId == dashboard.Id && r.DefinitionJson != null, cancellationToken);

        return new GetDashboardResponse(
            dashboard.Id,
            dashboard.Title,
            dashboard.GenerationStatus,
            dashboard.GenerationError,
            definition,
            CanRevert: revisionsWithDefinition >= 2,
            dashboard.GenerationRequestedAt,
            dashboard.GenerationCompletedAt);
    }
}
