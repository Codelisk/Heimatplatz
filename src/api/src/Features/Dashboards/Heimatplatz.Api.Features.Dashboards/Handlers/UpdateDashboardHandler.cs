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
/// Benennt eine Uebersicht um (der KI-Titel ist nur ein Vorschlag).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/dashboards")]
public class UpdateDashboardHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<UpdateDashboardRequest, UpdateDashboardResponse>
{
    [MediatorHttpPut("/", OperationId = "UpdateDashboard", RequiresAuthorization = true)]
    public async Task<UpdateDashboardResponse> Handle(
        UpdateDashboardRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.GetRequiredUserId();

        var title = request.Title.Trim();
        if (title.Length is < 1 or > 120)
            throw new ArgumentException("Der Titel muss zwischen 1 und 120 Zeichen lang sein.");

        var dashboard = await dbContext.Set<UserDashboard>()
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Übersicht mit ID {request.Id} nicht gefunden.");

        if (dashboard.UserId != userId)
            throw new UnauthorizedAccessException("Sie haben keine Berechtigung für diese Übersicht.");

        dashboard.Title = title;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateDashboardResponse(true);
    }
}
