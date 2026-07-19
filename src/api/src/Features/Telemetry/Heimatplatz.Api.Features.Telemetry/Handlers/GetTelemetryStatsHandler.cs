using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Models;
using Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Telemetry.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Telemetry.Handlers;

/// <summary>
/// Fehler-Statistik: Fehler/Warnungen pro Tag (UTC) und Top-Fehlergruppen im Zeitraum.
/// Gruppierung bewusst in-memory (Warning+-Menge ist ueberschaubar, Datums-Gruppierung
/// ist auf SQLite nicht uebersetzbar).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/telemetry")]
public class GetTelemetryStatsHandler(
    AppDbContext dbContext
) : IRequestHandler<GetTelemetryStatsRequest, GetTelemetryStatsResponse>
{
    private static readonly string[] WarningPlusLevels = ["Warning", "Error", "Critical"];

    [MediatorHttpGet("/stats", OperationId = "GetTelemetryStats", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireAdmin])]
    public async Task<GetTelemetryStatsResponse> Handle(GetTelemetryStatsRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var days = Math.Clamp(request.Days, 1, 90);
        var topGroupCount = Math.Clamp(request.TopGroupCount, 1, 50);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = new DateTimeOffset(today.AddDays(-(days - 1)).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var entries = await dbContext.Set<TelemetryLog>()
            .AsNoTracking()
            .Where(l => l.TimestampUtc >= from && WarningPlusLevels.Contains(l.Level))
            .Select(l => new { l.TimestampUtc, l.Level, l.ErrorGroupId })
            .ToListAsync(cancellationToken);

        var byDay = entries
            .GroupBy(e => DateOnly.FromDateTime(e.TimestampUtc.UtcDateTime))
            .ToDictionary(g => g.Key, g => g.ToList());

        var daily = new List<DailyErrorCountDto>(days);
        for (var day = today.AddDays(-(days - 1)); day <= today; day = day.AddDays(1))
        {
            var dayEntries = byDay.GetValueOrDefault(day);
            daily.Add(new DailyErrorCountDto(
                day,
                dayEntries?.Count(e => e.Level != "Warning") ?? 0,
                dayEntries?.Count(e => e.Level == "Warning") ?? 0));
        }

        var topGroupIds = entries
            .Where(e => e.ErrorGroupId != null)
            .GroupBy(e => e.ErrorGroupId!.Value)
            .OrderByDescending(g => g.Count())
            .Take(topGroupCount)
            .Select(g => g.Key)
            .ToList();

        var groups = await dbContext.Set<TelemetryErrorGroup>()
            .AsNoTracking()
            .Where(g => topGroupIds.Contains(g.Id))
            .ToListAsync(cancellationToken);

        // Reihenfolge der Haeufigkeit im Zeitfenster beibehalten
        var topGroups = topGroupIds
            .Select(id => groups.FirstOrDefault(g => g.Id == id))
            .Where(g => g != null)
            .Select(g => TelemetryQueryHelpers.Map(g!))
            .ToList();

        return new GetTelemetryStatsResponse(
            daily,
            topGroups,
            entries.Count(e => e.Level != "Warning"),
            entries.Count(e => e.Level == "Warning"));
    }
}
