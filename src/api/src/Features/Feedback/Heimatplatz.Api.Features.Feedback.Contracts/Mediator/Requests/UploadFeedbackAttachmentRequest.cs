using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;

/// <summary>
/// Laedt einen einzelnen Anhang (Bild oder Sprachnachricht) hoch, bevor die Anfrage bzw.
/// Nachricht erstellt wird. Ein Anhang pro Request (Originale koennen gross sein - gleiches
/// Muster wie der Immobilien-Foto-Upload).
/// </summary>
public record UploadFeedbackAttachmentRequest(
    string FileName,
    string ContentType,
    string Base64Data
) : IRequest<UploadFeedbackAttachmentResponse>;

/// <summary>Url ist absolut und wird beim Erstellen der Nachricht als Referenz mitgegeben.</summary>
public record UploadFeedbackAttachmentResponse(
    string Url,
    FeedbackAttachmentKind Kind,
    long FileSizeBytes);
