using Heimatplatz.Api.Features.Dashboards.Jobs;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace Heimatplatz.Api.Features.Dashboards.Services;

/// <summary>
/// Plant die Dashboard-Generierung als TickerQ-TimeTicker ein (sofortige
/// Ausfuehrung, Retries mit ansteigenden Intervallen). Der Job selbst ist in
/// <see cref="DashboardGenerationJob"/> registriert.
/// </summary>
public class TickerQDashboardGenerationJobScheduler(
    ITimeTickerManager<TimeTickerEntity> timeTickerManager
) : IDashboardGenerationJobScheduler
{
    public async Task ScheduleAsync(Guid revisionId, CancellationToken cancellationToken = default)
    {
        var result = await timeTickerManager.AddAsync(new TimeTickerEntity
        {
            Function = DashboardGenerationJob.FunctionName,
            Description = $"KI-Dashboard-Generierung fuer Revision {revisionId}",
            // Leicht in der Zukunft: ExecutionTime exakt "jetzt" laesst TickerQ den Job
            // als bereits ueberfaellig mit sofort gecanceltem Token starten
            // (gleiche Falle wie beim Beschreibungs-Job der PropertyDrafts).
            ExecutionTime = DateTime.UtcNow.AddSeconds(2),
            Request = TickerHelper.CreateTickerRequest(revisionId),
            Retries = DashboardGenerationJob.MaxRetries,
            RetryIntervals = DashboardGenerationJob.RetryIntervalsSeconds
        }, cancellationToken);

        if (!result.IsSucceeded)
            throw new InvalidOperationException(
                "Der Dashboard-Generierungs-Job konnte nicht eingeplant werden.", result.Exception);
    }
}
