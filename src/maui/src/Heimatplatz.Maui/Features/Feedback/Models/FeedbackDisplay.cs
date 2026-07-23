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

    public static string CategoryHint(FeedbackCategory category, FeedbackStringsLocalized loc) => category switch
    {
        FeedbackCategory.Problem => loc.TileProblemHint,
        FeedbackCategory.Question => loc.TileQuestionHint,
        FeedbackCategory.Praise => loc.TilePraiseHint,
        FeedbackCategory.Other => loc.TileOtherHint,
        _ => loc.TileIdeaHint
    };

    public static string CategoryIcon(FeedbackCategory category) => category switch
    {
        FeedbackCategory.Problem => "icon_alert_white.png",
        FeedbackCategory.Question => "icon_question_white.png",
        FeedbackCategory.Praise => "icon_thumbsup_white.png",
        FeedbackCategory.Other => "icon_dots_white.png",
        _ => "icon_bulb_white.png"
    };

    // Farbwerte == Colors.xaml (FeedbackIdea/Problem/Question/Praise/Other) - Items
    // entstehen im Code, daher hier gespiegelt statt Resource-Lookup
    public static Color CategoryColor(FeedbackCategory category) => category switch
    {
        FeedbackCategory.Problem => Color.FromArgb("#C25048"),
        FeedbackCategory.Question => Color.FromArgb("#2F6E9E"),
        FeedbackCategory.Praise => Color.FromArgb("#33854A"),
        FeedbackCategory.Other => Color.FromArgb("#7C7268"),
        _ => Color.FromArgb("#BE8A26")
    };

    /// <summary>Status-Ampel: Offen wartet (Honig), In Arbeit (Blau), Beantwortet (Gruen), Abgeschlossen (Grau).</summary>
    public static Color StatusColor(FeedbackTicketStatus status) => status switch
    {
        FeedbackTicketStatus.InProgress => Color.FromArgb("#2F6E9E"),
        FeedbackTicketStatus.Answered => Color.FromArgb("#33854A"),
        FeedbackTicketStatus.Closed => Color.FromArgb("#9A9088"),
        _ => Color.FromArgb("#BE8A26")
    };

    public static string FormatDate(DateTimeOffset value)
        => value.ToLocalTime().ToString("dd.MM.yyyy, HH:mm");

    /// <summary>"Heute, 14:32" / "Gestern, 09:05" / "12.07.2026" fuer die Anfragen-Liste.</summary>
    public static string FormatRelativeDate(DateTimeOffset value, FeedbackStringsLocalized loc)
    {
        var local = value.ToLocalTime();
        var today = DateTime.Today;

        if (local.Date == today)
            return loc.TodayFormat(local.ToString("HH:mm"));
        if (local.Date == today.AddDays(-1))
            return loc.YesterdayFormat(local.ToString("HH:mm"));

        return local.ToString("dd.MM.yyyy");
    }

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
