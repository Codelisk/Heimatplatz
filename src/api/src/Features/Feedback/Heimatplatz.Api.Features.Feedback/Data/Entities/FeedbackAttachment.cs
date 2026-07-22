using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Feedback.Contracts.Models;

namespace Heimatplatz.Api.Features.Feedback.Data.Entities;

/// <summary>
/// Anhang einer Feedback-Nachricht. Url ist der relative Upload-Pfad
/// (/uploads/feedback/...) - absolut wird sie erst in den Response-DTOs.
/// </summary>
public class FeedbackAttachment : BaseEntity
{
    public Guid MessageId { get; set; }
    public FeedbackMessage Message { get; set; } = null!;

    public FeedbackAttachmentKind Kind { get; set; }

    public required string Url { get; set; }

    public required string ContentType { get; set; }

    public long FileSizeBytes { get; set; }

    /// <summary>Aufnahmedauer bei Sprachnachrichten (vom Client gemeldet)</summary>
    public double? DurationSeconds { get; set; }

    public int SortOrder { get; set; }
}
