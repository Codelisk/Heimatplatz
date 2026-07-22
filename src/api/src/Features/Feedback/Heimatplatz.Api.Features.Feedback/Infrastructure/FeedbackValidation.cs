using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Heimatplatz.Api.Features.Feedback.Data.Entities;
using Heimatplatz.Api.Features.Feedback.Services;

namespace Heimatplatz.Api.Features.Feedback.Infrastructure;

/// <summary>Gemeinsame Eingabe-Validierung fuer Create- und AddMessage-Handler.</summary>
internal static class FeedbackValidation
{
    public const int MaxBodyLength = 4000;
    public const int MaxAttachmentsPerMessage = 10;

    /// <summary>Text ist Pflicht, ausser die Nachricht besteht nur aus Anhaengen (z.B. Sprachnachricht).</summary>
    public static void ValidateMessageInput(string? body, List<FeedbackAttachmentInput>? attachments)
    {
        var hasBody = !string.IsNullOrWhiteSpace(body);
        var attachmentCount = attachments?.Count ?? 0;

        if (!hasBody && attachmentCount == 0)
            throw new ArgumentException("Bitte gib eine Nachricht ein oder haenge etwas an.");

        if (body is { Length: > MaxBodyLength })
            throw new ArgumentException($"Die Nachricht ist zu lang (maximal {MaxBodyLength} Zeichen).");

        if (attachmentCount > MaxAttachmentsPerMessage)
            throw new ArgumentException($"Maximal {MaxAttachmentsPerMessage} Anhaenge pro Nachricht erlaubt.");
    }

    /// <summary>
    /// Loest die Anhang-Referenzen gegen die Dateien auf der Platte auf und baut die
    /// Entities (Reihenfolge = Reihenfolge der Eingabe).
    /// </summary>
    public static List<FeedbackAttachment> BuildAttachmentEntities(
        IFeedbackAttachmentService attachmentService,
        List<FeedbackAttachmentInput>? attachments)
    {
        if (attachments == null || attachments.Count == 0)
            return [];

        return attachments
            .Select((input, index) =>
            {
                var resolved = attachmentService.Resolve(input.Url);
                return new FeedbackAttachment
                {
                    Kind = resolved.Kind,
                    Url = resolved.RelativeUrl,
                    ContentType = resolved.ContentType,
                    FileSizeBytes = resolved.FileSizeBytes,
                    // Dauer ist nur bei Audio sinnvoll - Client-Angaben fuer Bilder ignorieren
                    DurationSeconds = resolved.Kind == FeedbackAttachmentKind.Audio ? input.DurationSeconds : null,
                    SortOrder = index
                };
            })
            .ToList();
    }
}
