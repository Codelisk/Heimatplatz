using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Telemetry.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Telemetry.Handlers;

/// <summary>
/// Fehlergruppen-Liste fuer die Auswertung: Filter auf Status/Zeit/Suchtext,
/// Sortierung nach letztem Auftreten (Default) oder Haeufigkeit.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/telemetry")]
public class ListErrorGroupsHandler(
    AppDbContext dbContext
) : IRequestHandler<ListErrorGroupsRequest, ListErrorGroupsResponse>
{
    [MediatorHttpGet("/error-groups", OperationId = "ListTelemetryErrorGroups", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireAdmin])]
    public async Task<ListErrorGroupsResponse> Handle(ListErrorGroupsRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = dbContext.Set<TelemetryErrorGroup>().AsNoTracking();

        if (request.Status is { } status)
        {
            query = query.Where(g => g.Status == status);
        }

        if (TelemetryQueryHelpers.TryParseTime(request.From, out var from))
        {
            query = query.Where(g => g.LastSeenUtc >= from);
        }

        if (TelemetryQueryHelpers.TryParseTime(request.To, out var to))
        {
            query = query.Where(g => g.LastSeenUtc <= to);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(g =>
                g.Title.ToLower().Contains(search)
                || g.ExceptionType.ToLower().Contains(search)
                || g.SampleMessage.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = string.Equals(request.Sort, "count", StringComparison.OrdinalIgnoreCase)
            ? query.OrderByDescending(g => g.OccurrenceCount)
            : query.OrderByDescending(g => g.LastSeenUtc);

        var groups = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new ListErrorGroupsResponse(
            groups.Select(TelemetryQueryHelpers.Map).ToList(),
            totalCount,
            page,
            pageSize);
    }
}
