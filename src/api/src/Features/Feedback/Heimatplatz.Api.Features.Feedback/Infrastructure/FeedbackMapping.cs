using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Heimatplatz.Api.Features.Feedback.Data.Entities;
using Microsoft.AspNetCore.Http;

namespace Heimatplatz.Api.Features.Feedback.Infrastructure;

/// <summary>
/// Gemeinsame DTO-Mappings und Text-Ableitungen der Feedback-Handler.
/// Baut u.a. absolute Anhang-URLs samt skalierter Bild-Vorschau (/api/images/local).
/// </summary>
internal static class FeedbackMapping
{
    /// <summary>Zielbreite der Bild-Vorschauen in Listen/Threads (wie Inserats-Karten)</summary>
    private const int ThumbnailWidth = 640;

    public static string GetBaseUrl(IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext ist nicht verfuegbar.");

        return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
    }

    public static FeedbackAttachmentDto ToDto(FeedbackAttachment attachment, string baseUrl)
    {
        var thumbnailUrl = attachment.Kind == FeedbackAttachmentKind.Image
            ? $"{baseUrl}/api/images/local?path={Uri.EscapeDataString(attachment.Url)}&w={ThumbnailWidth}"
            : null;

        return new FeedbackAttachmentDto(
            attachment.Id,
            attachment.Kind,
            $"{baseUrl}{attachment.Url}",
            thumbnailUrl,
            attachment.ContentType,
            attachment.FileSizeBytes,
            attachment.DurationSeconds);
    }

    public static FeedbackMessageDto ToDto(FeedbackMessage message, string baseUrl) => new(
        message.Id,
        message.Author,
        message.Body,
        message.CreatedAt,
        message.Attachments
            .OrderBy(a => a.SortOrder)
            .Select(a => ToDto(a, baseUrl))
            .ToList());

    /// <summary>Betreff aus der ersten Textzeile ableiten, Fallback auf die Kategorie.</summary>
    public static string DeriveSubject(string? subject, string body, FeedbackCategory category)
    {
        var trimmed = subject?.Trim();
        if (!string.IsNullOrEmpty(trimmed))
            return Truncate(trimmed, 200);

        var firstLine = body.Trim()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(firstLine))
            return Truncate(firstLine, 80);

        return category switch
        {
            FeedbackCategory.Idea => "Wunsch / Idee",
            FeedbackCategory.Problem => "Problem-Meldung",
            FeedbackCategory.Question => "Frage",
            FeedbackCategory.Praise => "Lob",
            _ => "Feedback"
        };
    }

    /// <summary>Einzeilige Kurzfassung einer Nachricht fuer Listenzeilen und Push-Texte.</summary>
    public static string BuildPreview(string body, bool hasAttachments, int maxLength = 120)
    {
        var text = string.Join(' ', body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (string.IsNullOrWhiteSpace(text))
            return hasAttachments ? "[Anhang]" : string.Empty;

        return Truncate(text, maxLength);
    }

    public static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)].TrimEnd() + "…";
}
