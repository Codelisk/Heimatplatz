namespace Heimatplatz.Api.Features.PropertyDrafts.Services;

/// <summary>
/// Plant den Hintergrund-Job fuer die KI-Beschreibungs-Generierung eines Entwurfs ein.
/// Produktiv via TickerQ (<see cref="TickerQDraftDescriptionJobScheduler"/>); ohne echte
/// Datenbank (Build-Zeit-OpenAPI-Generierung, Integrationstests mit InMemory-Provider)
/// wird der <see cref="NoOpDraftDescriptionJobScheduler"/> registriert.
/// </summary>
public interface IDraftDescriptionJobScheduler
{
    Task ScheduleAsync(Guid draftId, CancellationToken cancellationToken = default);
}
