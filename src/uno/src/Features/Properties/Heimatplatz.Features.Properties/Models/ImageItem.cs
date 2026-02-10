using Microsoft.UI.Xaml.Media.Imaging;

namespace Heimatplatz.Features.Properties.Models;

/// <summary>
/// Represents an image for display in the UI.
/// New images have byte[] data for upload; existing images have a Url.
/// </summary>
public record ImageItem(
    string FileName,
    string ContentType,
    byte[] Data,
    BitmapImage? Thumbnail = null,
    string? Url = null
)
{
    /// <summary>
    /// True if this image is already uploaded on the server (has URL, no data).
    /// </summary>
    public bool IsExisting => Url != null;

    /// <summary>
    /// Bindable image source: BitmapImage for new local images, URL string for existing server images.
    /// WinUI Image control accepts both types.
    /// </summary>
    public object? DisplaySource => IsExisting ? Url : Thumbnail;

    /// <summary>
    /// Creates a fresh MemoryStream from the byte[] data for upload.
    /// Call this each time you need to upload the image.
    /// </summary>
    public MemoryStream CreateStream() => new MemoryStream(Data);

    /// <summary>
    /// Returns the image data as Base64-encoded string for JSON upload.
    /// </summary>
    public string ToBase64() => Convert.ToBase64String(Data);
};
