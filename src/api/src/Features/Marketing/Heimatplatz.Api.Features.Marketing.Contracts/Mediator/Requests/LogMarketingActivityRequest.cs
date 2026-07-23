using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Haelt einen Schritt am Kontakt fest: Anruf, Notiz oder Termin - optional zusammen mit
/// einem Statuswechsel und einer Wiedervorlage. Das ist der Arbeitsschritt nach dem
/// Telefonat: "angerufen, Notiz dazu, Wiedervorlage naechste Woche, Status Wiedervorlage".
///
/// Wirkung auf den Kontakt:
/// - NewStatus gesetzt und abweichend  -> Status wird uebernommen, zusaetzlicher
///   StatusChange-Eintrag in der Historie
/// - FollowUpAt gesetzt                -> NextFollowUpAt wird gesetzt
/// - Type Call/Meeting                 -> LastContactedAt wird auf OccurredAt gesetzt
///
/// OccurredAt leer = jetzt (erlaubt das Nachtragen zurueckliegender Telefonate).
/// </summary>
public record LogMarketingActivityRequest(
    Guid ContactId,
    MarketingActivityType Type,
    string? Notes,
    DateTimeOffset? OccurredAt = null,
    DateTimeOffset? FollowUpAt = null,
    MarketingContactStatus? NewStatus = null
) : IRequest<LogMarketingActivityResponse>;

public record LogMarketingActivityResponse(bool Success, Guid? Id, string? Error);
