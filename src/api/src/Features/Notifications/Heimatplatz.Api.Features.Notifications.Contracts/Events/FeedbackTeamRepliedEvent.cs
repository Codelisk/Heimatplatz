using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Contracts.Events;

/// <summary>
/// Wird publiziert, wenn das Team im Intern-Bereich auf eine Feedback-Anfrage antwortet.
/// Das Notifications-Feature schickt daraufhin eine Push-Benachrichtigung an alle
/// Geraete des Nutzers (Deep-Link in den Anfrage-Verlauf).
/// </summary>
/// <param name="TicketId">ID der Feedback-Anfrage</param>
/// <param name="UserId">Besitzer der Anfrage (Push-Empfaenger)</param>
/// <param name="Subject">Betreff der Anfrage</param>
/// <param name="BodyPreview">Einzeilige Kurzfassung der Antwort</param>
public record FeedbackTeamRepliedEvent(
    Guid TicketId,
    Guid UserId,
    string Subject,
    string BodyPreview
) : IEvent;
