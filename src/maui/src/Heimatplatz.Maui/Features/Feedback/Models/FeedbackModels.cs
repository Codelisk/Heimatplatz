using CommunityToolkit.Mvvm.ComponentModel;
using Heimatplatz.Maui.ApiClient.Generated;

namespace Heimatplatz.Maui.Features.Feedback.Models;

/// <summary>Zeile in "Meine Anfragen" (fertig aufbereitete Anzeige-Texte).</summary>
public class FeedbackTicketListItem
{
    public required Guid Id { get; init; }
    public required string Subject { get; init; }
    public required string Preview { get; init; }
    public required string StatusLabel { get; init; }
    public required string CategoryLabel { get; init; }
    public required string DateText { get; init; }
    public required string MessageCountText { get; init; }
    public bool HasUnread { get; init; }
    public bool HasPreview => Preview.Length > 0;
}

/// <summary>Auswahlkarte im Compose (Kategorie-Chip).</summary>
public partial class FeedbackCategoryOption : ObservableObject
{
    public required FeedbackCategory Value { get; init; }
    public required string Label { get; init; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

/// <summary>
/// Angehaengtes Foto im Compose. Das Original liegt als Datei im App-Cache
/// (MediaFileCache), gerendert wird nur die kleine EXIF-korrigierte Vorschau
/// (PhotoPreviewService) - nie das Original (Android-Canvas-Limit).
/// </summary>
public partial class FeedbackPhotoItem : ObservableObject
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }

    [ObservableProperty]
    public partial ImageSource? Preview { get; set; }
}

/// <summary>Bild-Anhang in einer Verlaufs-Nachricht (remote URLs).</summary>
public class FeedbackImageItem
{
    public required string ThumbnailUrl { get; init; }
    public required string FullUrl { get; init; }
}

/// <summary>Sprachnachricht in einer Verlaufs-Nachricht (Play-Zustand fuer den Button).</summary>
public partial class FeedbackAudioItem : ObservableObject
{
    public required string Url { get; init; }
    public required string Label { get; init; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }
}

/// <summary>Chat-Blase im Verlauf.</summary>
public class FeedbackMessageItem
{
    public required bool IsUser { get; init; }
    public required string AuthorLabel { get; init; }
    public required string DateText { get; init; }
    public required string Body { get; init; }
    public bool HasBody => Body.Length > 0;
    public required List<FeedbackImageItem> Images { get; init; }
    public required List<FeedbackAudioItem> Audios { get; init; }
    public bool HasImages => Images.Count > 0;
    public bool HasAudios => Audios.Count > 0;
}
