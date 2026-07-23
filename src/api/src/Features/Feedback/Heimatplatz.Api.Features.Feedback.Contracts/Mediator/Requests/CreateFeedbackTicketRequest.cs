using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;

/// <summary>
/// Erstellt eine neue Feedback-Anfrage mit erster Nachricht. Der Titel wird IMMER
/// serverseitig aus Kategorie + fortlaufender Nummer gebildet (z.B. "Lob 1") - der
/// Nutzer waehlt beim Erstellen nur die Kategorie, umbenennen kann er spaeter ueber
/// RenameFeedbackTicket. Attachments referenzieren zuvor via UploadFeedbackAttachment
/// hochgeladene Dateien.
/// </summary>
public record CreateFeedbackTicketRequest(
    FeedbackCategory Category,
    string Body,
    FeedbackSource Source,
    string? AppVersion,
    List<FeedbackAttachmentInput>? Attachments
) : IRequest<CreateFeedbackTicketResponse>;

public record CreateFeedbackTicketResponse(Guid TicketId, Guid MessageId);
