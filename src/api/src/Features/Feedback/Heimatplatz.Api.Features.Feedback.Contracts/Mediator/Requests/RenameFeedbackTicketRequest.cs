using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;

/// <summary>
/// Nutzer benennt den Auto-Titel einer eigenen Anfrage um (z.B. "Lob 1" -> eigener Titel).
/// Nur der Ersteller der Anfrage darf das.
/// </summary>
public record RenameFeedbackTicketRequest(
    Guid TicketId,
    string Subject
) : IRequest<RenameFeedbackTicketResponse>;

public record RenameFeedbackTicketResponse(bool Success, string? Error);
