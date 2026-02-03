using Microsoft.UI.Xaml.Media.Imaging;

namespace Heimatplatz.Features.Properties.Models;

/// <summary>
/// Represents an image for display in the UI.
/// New images have a Stream for upload; existing images have a Url.
/// </summary>
public record ImageItem(
    string FileName,
    string ContentType,
    Stream Stream,
    BitmapImage? Thumbnail = null,
    string? Url = null
)
{
    /// <summary>
    /// True if this image is already uploaded on the server (has URL, no stream data).
    /// </summary>
    public bool IsExisting => Url != null;

    /// <summary>
    /// Bindable image source: BitmapImage for new local images, URL string for existing server images.
    /// WinUI Image control accepts both types.
    /// </summary>
    public object? DisplaySource => IsExisting ? Url : Thumbnail;
};
