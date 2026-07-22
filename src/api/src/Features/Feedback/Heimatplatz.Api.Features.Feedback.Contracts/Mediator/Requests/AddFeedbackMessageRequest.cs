using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;

/// <summary>
/// Haengt eine weitere Nutzer-Nachricht an eine bestehende Anfrage an.
/// Beantwortete/geschlossene Anfragen werden dadurch wieder geoeffnet.
/// </summary>
public record AddFeedbackMessageRequest(
    Guid TicketId,
    string Body,
    List<FeedbackAttachmentInput>? Attachments
) : IRequest<AddFeedbackMessageResponse>;

/// <summary>Message ist null, wenn das Ticket nicht existiert oder nicht dem Nutzer gehoert.</summary>
public record AddFeedbackMessageResponse(FeedbackMessageDto? Message);
