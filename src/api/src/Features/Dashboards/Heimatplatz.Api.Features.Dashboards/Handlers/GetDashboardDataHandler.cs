using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Dashboards.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Heimatplatz.Api.Features.Dashboards.Data.Entities;
using Heimatplatz.Api.Features.Dashboards.Infrastructure;
using Heimatplatz.Api.Features.Dashboards.Services.Widgets;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Handlers;

/// <summary>
/// Daten-Ebene: loest die Queries aller Widgets der gespeicherten Definition
/// serverseitig auf. Sequentiell (ein DbContext pro Scope), fail-soft je Widget -
/// eine kaputte Quelle reisst nie die ganze Uebersicht (Vorbild /intern-Dashboard).
/// Unbekannte Widget-Arten werden tolerant uebersprungen (aeltere Definitionen
/// nach einem Katalog-Rueckbau).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/dashboards")]
public class GetDashboardDataHandler(
    AppDbContext dbContext,
    IEnumerable<IDashboardWidgetResolver> resolvers,
    IHttpContextAccessor httpContextAccessor,
    ILogger<GetDashboardDataHandler> logger
) : IRequestHandler<GetDashboardDataRequest, GetDashboardDataResponse>
{
    public const string WidgetErrorMessage = "Dieser Bereich konnte gerade nicht geladen werden.";

    [MediatorHttpGet("/{Id}/data", OperationId = "GetDashboardData", RequiresAuthorization = true)]
    public async Task<GetDashboardDataResponse> Handle(
        GetDashboardDataRequest request,
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
        if (definition is null)
            return new GetDashboardDataResponse([]);

        var resolverByKind = resolvers.ToDictionary(r => r.Kind, StringComparer.OrdinalIgnoreCase);
        var results = new List<WidgetDataDto>(definition.Widgets.Count);
        var resolveContext = new WidgetResolveContext(Math.Max(request.Page, 0));

        foreach (var widget in definition.Widgets)
        {
            if (!resolverByKind.TryGetValue(widget.Kind, out var resolver))
                continue;

            try
            {
                results.Add(await resolver.ResolveAsync(widget, resolveContext, cancellationToken));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[Dashboards] Widget {WidgetId} ({Kind}) von Dashboard {DashboardId} konnte nicht aufgeloest werden",
                    widget.Id, widget.Kind, dashboard.Id);
                results.Add(new WidgetDataDto(widget.Id, widget.Kind, Success: false, Error: WidgetErrorMessage));
            }
        }

        return new GetDashboardDataResponse(results);
    }
}
