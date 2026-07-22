using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Localization.Feedback;

namespace Heimatplatz.Maui.Features.Feedback.Models;

/// <summary>Gemeinsame Anzeige-Aufbereitung der Feedback-Seiten (Labels, Datums-/Dauerformate).</summary>
internal static class FeedbackDisplay
{
    public static string StatusLabel(FeedbackTicketStatus status, FeedbackStringsLocalized loc) => status switch
    {
        FeedbackTicketStatus.InProgress => loc.StatusInProgress,
        FeedbackTicketStatus.Answered => loc.StatusAnswered,
        FeedbackTicketStatus.Closed => loc.StatusClosed,
        _ => loc.StatusOpen
    };

    public static string CategoryLabel(FeedbackCategory category, FeedbackStringsLocalized loc) => category switch
    {
        FeedbackCategory.Problem => loc.CategoryProblem,
        FeedbackCategory.Question => loc.CategoryQuestion,
        FeedbackCategory.Praise => loc.CategoryPraise,
        FeedbackCategory.Other => loc.CategoryOther,
        _ => loc.CategoryIdea
    };

    public static string FormatDate(DateTimeOffset value)
        => value.ToLocalTime().ToString("dd.MM.yyyy, HH:mm");

    /// <summary>"1:07" fuer 67 Sekunden.</summary>
    public static string FormatDuration(double seconds)
    {
        var total = (int)Math.Round(seconds);
        return $"{total / 60}:{total % 60:D2}";
    }

    public static FeedbackMessageItem ToMessageItem(FeedbackMessageDto message, FeedbackStringsLocalized loc)
    {
        var images = new List<FeedbackImageItem>();
        var audios = new List<FeedbackAudioItem>();

        foreach (var attachment in message.Attachments ?? [])
        {
            if (attachment.Kind == FeedbackAttachmentKind.Image)
            {
                images.Add(new FeedbackImageItem
                {
                    ThumbnailUrl = attachment.ThumbnailUrl ?? attachment.Url,
                    FullUrl = attachment.Url
                });
            }
            else
            {
                var label = attachment.DurationSeconds is > 0
                    ? $"{loc.VoiceMessage} · {FormatDuration(attachment.DurationSeconds.Value)}"
                    : loc.VoiceMessage;
                audios.Add(new FeedbackAudioItem { Url = attachment.Url, Label = label });
            }
        }

        return new FeedbackMessageItem
        {
            IsUser = message.Author == FeedbackAuthor.User,
            AuthorLabel = message.Author == FeedbackAuthor.User ? loc.You : loc.Team,
            DateText = FormatDate(message.CreatedAt),
            Body = message.Body ?? string.Empty,
            Images = images,
            Audios = audios
        };
    }
}
