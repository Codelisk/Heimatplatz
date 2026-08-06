namespace Heimatplatz.Api.Features.Dashboards.Services;

/// <summary>
/// Plant den Hintergrund-Job fuer eine Dashboard-Generierungsrunde ein
/// (Payload = Revision-Id, die Revision traegt Wunsch und Dashboard-Bezug).
/// Produktiv via TickerQ (<see cref="TickerQDashboardGenerationJobScheduler"/>);
/// ohne echte Datenbank (Build-Zeit-OpenAPI-Generierung, Integrationstests mit
/// InMemory-Provider) wird der <see cref="NoOpDashboardGenerationJobScheduler"/> registriert.
/// </summary>
public interface IDashboardGenerationJobScheduler
{
    Task ScheduleAsync(Guid revisionId, CancellationToken cancellationToken = default);
}
