using Heimatplatz.Maui.ApiClient.Generated;

namespace Heimatplatz.Maui.Features.Feedback.Services;

/// <summary>
/// Duenner Wrapper um die generierten Feedback-HTTP-Requests (Shiny.Mediator).
/// Anhaenge werden VOR dem Erstellen einzeln als Base64 hochgeladen und dann
/// per URL referenziert (gleiches Muster wie der Inserats-Foto-Upload).
/// </summary>
public interface IFeedbackService
{
    Task<GetMyFeedbackTicketsResponse> GetMyTicketsAsync(CancellationToken ct = default);

    /// <summary>Liefert null, wenn das Ticket nicht existiert oder nicht dem Nutzer gehoert.</summary>
    Task<FeedbackTicketDetailDto?> GetTicketAsync(Guid ticketId, CancellationToken ct = default);

    Task<UploadFeedbackAttachmentResponse> UploadAttachmentAsync(
        string fileName, string contentType, byte[] bytes, CancellationToken ct = default);

    Task<CreateFeedbackTicketResponse> CreateTicketAsync(
        FeedbackCategory category,
        string body,
        List<FeedbackAttachmentInput> attachments,
        CancellationToken ct = default);

    /// <summary>Liefert null, wenn das Ticket nicht existiert oder nicht dem Nutzer gehoert.</summary>
    Task<FeedbackMessageDto?> AddMessageAsync(
        Guid ticketId, string body, List<FeedbackAttachmentInput> attachments, CancellationToken ct = default);

    /// <summary>Benennt den Auto-Titel einer eigenen Anfrage um. Liefert false bei Fehler (z.B. leerer Titel).</summary>
    Task<bool> RenameTicketAsync(Guid ticketId, string subject, CancellationToken ct = default);
}
