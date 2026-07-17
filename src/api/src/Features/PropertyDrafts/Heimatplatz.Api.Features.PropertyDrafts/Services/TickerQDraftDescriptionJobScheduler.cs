using Heimatplatz.Api.Features.PropertyDrafts.Jobs;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace Heimatplatz.Api.Features.PropertyDrafts.Services;

/// <summary>
/// Plant die Beschreibungs-Generierung als TickerQ-TimeTicker ein (sofortige Ausfuehrung,
/// Retries mit ansteigenden Intervallen). Der Job selbst ist in <see cref="DraftDescriptionJob"/>
/// registriert.
/// </summary>
public class TickerQDraftDescriptionJobScheduler(
    ITimeTickerManager<TimeTickerEntity> timeTickerManager
) : IDraftDescriptionJobScheduler
{
    public async Task ScheduleAsync(Guid draftId, CancellationToken cancellationToken = default)
    {
        var result = await timeTickerManager.AddAsync(new TimeTickerEntity
        {
            Function = DraftDescriptionJob.FunctionName,
            Description = $"KI-Beschreibung fuer Inserat-Entwurf {draftId}",
            // Leicht in der Zukunft: ExecutionTime exakt "jetzt" laesst TickerQ den Job
            // als bereits ueberfaellig mit sofort gecanceltem Token starten
            // (TaskCanceledException beim Request-Fetch, E2E beobachtet).
            ExecutionTime = DateTime.UtcNow.AddSeconds(2),
            Request = TickerHelper.CreateTickerRequest(draftId),
            Retries = DraftDescriptionJob.MaxRetries,
            RetryIntervals = DraftDescriptionJob.RetryIntervalsSeconds
        }, cancellationToken);

        if (!result.IsSucceeded)
            throw new InvalidOperationException(
                "Der Beschreibungs-Job konnte nicht eingeplant werden.", result.Exception);
    }
}
