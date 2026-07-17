using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.PropertyDrafts.Services;

/// <summary>
/// Fallback ohne TickerQ (InMemory-Datenbank in Build-Tools/Integrationstests):
/// der Entwurf bleibt auf Status Queued stehen, es laeuft kein Job.
/// Integrationstests rufen den <see cref="DraftDescriptionProcessor"/> direkt auf.
/// </summary>
public class NoOpDraftDescriptionJobScheduler(
    ILogger<NoOpDraftDescriptionJobScheduler> logger
) : IDraftDescriptionJobScheduler
{
    public Task ScheduleAsync(Guid draftId, CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "[PropertyDrafts] Hintergrund-Jobs sind deaktiviert (keine echte Datenbank) - " +
            "Beschreibungs-Job fuer Entwurf {DraftId} wird NICHT ausgefuehrt.", draftId);
        return Task.CompletedTask;
    }
}
