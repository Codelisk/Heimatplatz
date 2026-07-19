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
/// Log-Suche fuer die Auswertung: Level-/Trace-/Text-/Zeit-/Quellen-Filter,
/// paged, neueste zuerst.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/telemetry")]
public class SearchTelemetryLogsHandler(
    AppDbContext dbContext
) : IRequestHandler<SearchTelemetryLogsRequest, SearchTelemetryLogsResponse>
{
    [MediatorHttpGet("/logs", OperationId = "SearchTelemetryLogs", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireAdmin])]
    public async Task<SearchTelemetryLogsResponse> Handle(SearchTelemetryLogsRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 500);

        var query = dbContext.Set<TelemetryLog>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.MinLevel))
        {
            var levels = TelemetryQueryHelpers.LevelsAtOrAbove(request.MinLevel);
            query = query.Where(l => levels.Contains(l.Level));
        }

        if (!string.IsNullOrWhiteSpace(request.TraceId))
        {
            var traceId = request.TraceId.ToLowerInvariant();
            query = query.Where(l => l.TraceId == traceId);
        }

        if (request.Source is { } source)
        {
            query = query.Where(l => l.Source == source);
        }

        if (request.ErrorGroupId is { } groupId)
        {
            query = query.Where(l => l.ErrorGroupId == groupId);
        }

        if (TelemetryQueryHelpers.TryParseTime(request.From, out var from))
        {
            query = query.Where(l => l.TimestampUtc >= from);
        }

        if (TelemetryQueryHelpers.TryParseTime(request.To, out var to))
        {
            query = query.Where(l => l.TimestampUtc <= to);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(l =>
                l.Message.ToLower().Contains(search)
                || l.Category.ToLower().Contains(search)
                || (l.ExceptionType != null && l.ExceptionType.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var logs = await query
            .OrderByDescending(l => l.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new SearchTelemetryLogsResponse(
            logs.Select(TelemetryQueryHelpers.Map).ToList(),
            totalCount,
            page,
            pageSize);
    }
}
