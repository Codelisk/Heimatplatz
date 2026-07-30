using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Schnellaktion der Akquise-Arbeitsliste: ein Klick im Intern-Bereich buendelt
/// Statuswechsel, Grund und Wiedervorlage zu einem Aufruf - die Kontaktliste kommt so
/// ohne Formular aus. Der Grund landet als Notiz direkt am StatusChange-Eintrag der
/// Timeline (bzw. als Anruf-/Wiedervorlage-Notiz).
///
/// Wirkung je Aktion:
/// - Interested            -> Status Interessiert (Reason optional als Notiz)
/// - Reject                -> Status Kein Interesse, Reason = Ablehngrund
/// - Block                 -> Status Nicht kontaktieren (Sperrliste), Reason optional
/// - Snooze                -> NextFollowUpAt = FollowUpAt (Pflicht); Arbeitsstatus
///                            (ToContact/Contacted/Replied) wandert mit auf FollowUp
/// - NotReached            -> Anruf-Aktivitaet (Kontaktversuch, LastContactedAt und
///                            Status bleiben unveraendert), optional neuer Termin
/// - Restore               -> Rueckgaengig: RestoreStatus/RestoreFollowUpAt (aus der
///                            vorherigen Antwort) werden wiederhergestellt
///
/// Die Antwort traegt den vorherigen Zustand (PreviousStatus/PreviousFollowUpAt),
/// damit das Web ein "Rueckgaengig" ohne weiteren Roundtrip anbieten kann.
/// </summary>
public record QuickMarketingContactActionRequest(
    Guid ContactId,
    MarketingQuickAction Action,
    string? Reason = null,
    DateTimeOffset? FollowUpAt = null,
    MarketingContactStatus? RestoreStatus = null,
    DateTimeOffset? RestoreFollowUpAt = null
) : IRequest<QuickMarketingContactActionResponse>;

public record QuickMarketingContactActionResponse(
    bool Success,
    string? Error,
    MarketingContactStatus? Status,
    DateTimeOffset? NextFollowUpAt,
    MarketingContactStatus? PreviousStatus,
    DateTimeOffset? PreviousFollowUpAt
);
