using Heimatplatz.Api.Features.Feedback.Contracts.Models;

namespace Heimatplatz.Api.Features.Feedback.Services;

/// <summary>Gespeicherter Anhang: relative URL + abgeleitete Metadaten.</summary>
public record SavedFeedbackAttachment(string RelativeUrl, FeedbackAttachmentKind Kind, long FileSizeBytes);

/// <summary>Aufgeloeste Anhang-Referenz aus einer Create-/AddMessage-Anfrage.</summary>
public record ResolvedFeedbackAttachment(string RelativeUrl, FeedbackAttachmentKind Kind, string ContentType, long FileSizeBytes);

/// <summary>
/// Speichert und verwaltet Feedback-Anhaenge (Bilder und Sprachnachrichten)
/// unter wwwroot/uploads/feedback.
/// </summary>
public interface IFeedbackAttachmentService
{
    /// <summary>
    /// Speichert einen Base64-Anhang. Bilder werden wie bei Inseraten als Original plus
    /// Anzeige-Variante abgelegt (zurueckgegeben wird die Varianten-URL), Audio unveraendert.
    /// </summary>
    Task<SavedFeedbackAttachment> SaveBase64Async(string fileName, string contentType, string base64Data, CancellationToken ct = default);

    /// <summary>
    /// Validiert eine Anhang-Referenz (URL aus einem vorherigen Upload) und leitet
    /// Art, Content-Type und Groesse aus der Datei auf der Platte ab.
    /// Wirft ArgumentException bei fremden Pfaden oder fehlenden Dateien.
    /// </summary>
    ResolvedFeedbackAttachment Resolve(string url);

    /// <summary>Loescht die Dateien der angegebenen Anhang-URLs (inkl. Varianten ueber den GUID-Stamm).</summary>
    Task DeleteAttachmentFilesAsync(IEnumerable<string> urls, CancellationToken ct = default);
}
