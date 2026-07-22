using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;

/// <summary>
/// Intern: Team-Antwort auf eine Anfrage. Setzt den Status auf Answered und loest
/// eine Push-Benachrichtigung an alle Geraete des Nutzers aus (Deep-Link in den Verlauf).
/// </summary>
public record ReplyToFeedbackTicketRequest(
    Guid TicketId,
    string Body
) : IRequest<ReplyToFeedbackTicketResponse>;

public record ReplyToFeedbackTicketResponse(bool Success, Guid? MessageId, string? Error);
