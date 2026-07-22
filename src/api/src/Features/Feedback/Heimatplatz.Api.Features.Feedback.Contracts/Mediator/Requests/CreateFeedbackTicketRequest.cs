using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;

/// <summary>
/// Erstellt eine neue Feedback-Anfrage mit erster Nachricht. Subject ist optional -
/// fehlt es, leitet der Server es aus der ersten Textzeile bzw. der Kategorie ab.
/// Attachments referenzieren zuvor via UploadFeedbackAttachment hochgeladene Dateien.
/// </summary>
public record CreateFeedbackTicketRequest(
    FeedbackCategory Category,
    string? Subject,
    string Body,
    FeedbackSource Source,
    string? AppVersion,
    List<FeedbackAttachmentInput>? Attachments
) : IRequest<CreateFeedbackTicketResponse>;

public record CreateFeedbackTicketResponse(Guid TicketId, Guid MessageId);
