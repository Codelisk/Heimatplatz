namespace Heimatplatz.Api.Features.Feedback.Contracts.Models;

/// <summary>
/// Anhang einer Feedback-Nachricht. Url ist absolut; ThumbnailUrl liefert bei Bildern
/// die ueber /api/images/local skalierte Vorschau (640px), bei Audio null.
/// </summary>
public record FeedbackAttachmentDto(
    Guid Id,
    FeedbackAttachmentKind Kind,
    string Url,
    string? ThumbnailUrl,
    string ContentType,
    long FileSizeBytes,
    double? DurationSeconds);

/// <summary>Eine Nachricht im Feedback-Verlauf (Nutzer oder Team).</summary>
public record FeedbackMessageDto(
    Guid Id,
    FeedbackAuthor Author,
    string Body,
    DateTimeOffset CreatedAt,
    List<FeedbackAttachmentDto> Attachments);

/// <summary>Listenzeile "Meine Anfragen" (Nutzer-Sicht).</summary>
public record FeedbackTicketSummaryDto(
    Guid Id,
    FeedbackCategory Category,
    string Subject,
    FeedbackTicketStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastMessageAt,
    bool HasUnread,
    int MessageCount,
    string LastMessagePreview);

/// <summary>Kompletter Verlauf einer Anfrage (Nutzer-Sicht, Nachrichten chronologisch).</summary>
public record FeedbackTicketDetailDto(
    Guid Id,
    FeedbackCategory Category,
    string Subject,
    FeedbackTicketStatus Status,
    DateTimeOffset CreatedAt,
    List<FeedbackMessageDto> Messages);

/// <summary>Listenzeile im Intern-Bereich (mit Nutzer- und Plattform-Infos).</summary>
public record AdminFeedbackTicketSummaryDto(
    Guid Id,
    FeedbackCategory Category,
    string Subject,
    FeedbackTicketStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastMessageAt,
    bool HasUnreadFromUser,
    int MessageCount,
    string LastMessagePreview,
    Guid UserId,
    string? UserName,
    string? UserEmail,
    FeedbackSource Source,
    string? AppVersion);

/// <summary>Kompletter Verlauf im Intern-Bereich (Nachrichten chronologisch).</summary>
public record AdminFeedbackTicketDetailDto(
    Guid Id,
    FeedbackCategory Category,
    string Subject,
    FeedbackTicketStatus Status,
    DateTimeOffset CreatedAt,
    Guid UserId,
    string? UserName,
    string? UserEmail,
    FeedbackSource Source,
    string? AppVersion,
    List<FeedbackMessageDto> Messages);

/// <summary>
/// Referenz auf einen zuvor hochgeladenen Anhang (Url aus UploadFeedbackAttachmentResponse).
/// Art, Content-Type und Groesse leitet der Server aus der Datei ab - nur die Audio-Dauer
/// kennt allein der Client (Aufnahmedauer).
/// </summary>
public record FeedbackAttachmentInput(string Url, double? DurationSeconds);
