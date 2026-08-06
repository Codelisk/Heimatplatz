using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.Dashboards.Services;

/// <summary>
/// Fallback ohne TickerQ (InMemory-Datenbank in Build-Tools/Integrationstests):
/// das Dashboard bleibt auf Status Queued stehen, es laeuft kein Job.
/// Integrationstests rufen den <see cref="DashboardGenerationProcessor"/> direkt auf.
/// </summary>
public class NoOpDashboardGenerationJobScheduler(
    ILogger<NoOpDashboardGenerationJobScheduler> logger
) : IDashboardGenerationJobScheduler
{
    public Task ScheduleAsync(Guid revisionId, CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "[Dashboards] Hintergrund-Jobs sind deaktiviert (keine echte Datenbank) - " +
            "Generierungs-Job fuer Revision {RevisionId} wird NICHT ausgefuehrt.", revisionId);
        return Task.CompletedTask;
    }
}
