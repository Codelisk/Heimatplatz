using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Heimatplatz.Api.Features.Feedback.Data.Entities;
using Heimatplatz.Api.Features.Properties.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Heimatplatz.Api.Features.Feedback.Infrastructure;

/// <summary>
/// Gemeinsame DTO-Mappings und Text-Ableitungen der Feedback-Handler.
/// Baut u.a. absolute Anhang-URLs samt skalierter Bild-Vorschau (/api/images/local).
/// </summary>
internal static class FeedbackMapping
{
    /// <summary>Zielbreite der Bild-Vorschauen in Listen/Threads (wie Inserats-Karten)</summary>
    private const int ThumbnailWidth = 640;

    // Absolute Anhang-URLs muessen browser-erreichbar sein: bevorzugt Api:PublicBaseUrl
    // statt Scheme+Host der Anfrage - der Astro-SSR-Server ruft die Admin-Endpoints ueber
    // das interne Docker-Netz (http://api:8080) auf, dessen Host im Browser nicht aufloest.
    public static string GetBaseUrl(IHttpContextAccessor httpContextAccessor, IConfiguration configuration) =>
        GetPropertiesHandler.ResolveApiBaseUrl(httpContextAccessor, configuration);

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

    /// <summary>
    /// Auto-Titel aus Kategorie + fortlaufender Nummer (z.B. "Lob 1"). Der Nutzer waehlt
    /// beim Erstellen nur die Kategorie - <paramref name="sequenceNumber"/> ist die Anzahl
    /// bisheriger eigener Anfragen dieser Kategorie + 1. Umbenennen erfolgt spaeter separat.
    /// </summary>
    public static string BuildAutoSubject(FeedbackCategory category, int sequenceNumber)
    {
        var label = category switch
        {
            FeedbackCategory.Idea => "Wunsch / Idee",
            FeedbackCategory.Problem => "Problem-Meldung",
            FeedbackCategory.Question => "Frage",
            FeedbackCategory.Praise => "Lob",
            _ => "Feedback"
        };
        return $"{label} {sequenceNumber}";
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
