using Shiny;

namespace Heimatplatz.Maui.Core.Media;

/// <summary>
/// Plattform-Implementierung: nutzt jeweils die nativen Decoder mit
/// Subsampling, damit nie das komplette Original in den Speicher dekodiert
/// wird (Android: BitmapFactory.InSampleSize, iOS: ImageIO-Thumbnail,
/// Windows: BitmapDecoder mit BitmapTransform).
/// </summary>
[Singleton]
public class PhotoPreviewService : IPhotoPreviewService
{
    private const int JpegQuality = 80;

    public async Task<byte[]> CreatePreviewAsync(string filePath, int maxDimension = 1600, CancellationToken ct = default)
    {
        try
        {
            var preview = await CreatePlatformPreviewAsync(filePath, maxDimension, ct);
            if (preview is { Length: > 0 })
                return preview;
        }
        catch (Exception)
        {
            // Vorschau ist Komfort - ein nicht dekodierbares Foto (z.B. exotisches
            // Format) faellt unten auf die Originaldatei zurueck
        }

        return await File.ReadAllBytesAsync(filePath, ct);
    }

#if ANDROID
    private static async Task<byte[]?> CreatePlatformPreviewAsync(string filePath, int maxDimension, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            // Nur die Bildmasse lesen, dann mit InSampleSize dekodieren -
            // so landet ein 100-MP-Foto nie komplett im Speicher
            var bounds = new Android.Graphics.BitmapFactory.Options { InJustDecodeBounds = true };
            Android.Graphics.BitmapFactory.DecodeFile(filePath, bounds);
            if (bounds.OutWidth <= 0 || bounds.OutHeight <= 0)
                return null;

            var sampleSize = 1;
            while (Math.Max(bounds.OutWidth, bounds.OutHeight) / (sampleSize * 2) >= maxDimension)
                sampleSize *= 2;

            var options = new Android.Graphics.BitmapFactory.Options { InSampleSize = sampleSize };
            var bitmap = Android.Graphics.BitmapFactory.DecodeFile(filePath, options);
            if (bitmap == null)
                return null;

            try
            {
                bitmap = ApplyExifOrientation(filePath, bitmap);

                using var stream = new MemoryStream();
                bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg!, JpegQuality, stream);
                return stream.ToArray();
            }
            finally
            {
                bitmap.Recycle();
                bitmap.Dispose();
            }
        }, ct);
    }

    private static Android.Graphics.Bitmap ApplyExifOrientation(string filePath, Android.Graphics.Bitmap bitmap)
    {
        var exif = new Android.Media.ExifInterface(filePath);
        var orientation = (Android.Media.Orientation)exif.GetAttributeInt(
            Android.Media.ExifInterface.TagOrientation,
            (int)Android.Media.Orientation.Normal);

        using var matrix = new Android.Graphics.Matrix();
        switch (orientation)
        {
            case Android.Media.Orientation.Rotate90:
                matrix.PostRotate(90);
                break;
            case Android.Media.Orientation.Rotate180:
                matrix.PostRotate(180);
                break;
            case Android.Media.Orientation.Rotate270:
                matrix.PostRotate(270);
                break;
            case Android.Media.Orientation.FlipHorizontal:
                matrix.PostScale(-1, 1);
                break;
            case Android.Media.Orientation.FlipVertical:
                matrix.PostScale(1, -1);
                break;
            case Android.Media.Orientation.Transpose:
                matrix.PostRotate(90);
                matrix.PostScale(-1, 1);
                break;
            case Android.Media.Orientation.Transverse:
                matrix.PostRotate(270);
                matrix.PostScale(-1, 1);
                break;
            default:
                return bitmap;
        }

        var rotated = Android.Graphics.Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, matrix, filter: true);
        if (!ReferenceEquals(rotated, bitmap))
        {
            bitmap.Recycle();
            bitmap.Dispose();
        }

        return rotated;
    }
#elif IOS || MACCATALYST
    private static Task<byte[]?> CreatePlatformPreviewAsync(string filePath, int maxDimension, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            using var source = ImageIO.CGImageSource.FromUrl(Foundation.NSUrl.FromFilename(filePath));
            if (source == null)
                return null;

            var options = new ImageIO.CGImageThumbnailOptions
            {
                CreateThumbnailFromImageAlways = true,
                // EXIF-Orientierung direkt beim Erzeugen anwenden
                CreateThumbnailWithTransform = true,
                MaxPixelSize = maxDimension,
                ShouldCacheImmediately = true
            };

            using var cgImage = source.CreateThumbnail(0, options);
            if (cgImage == null)
                return null;

            using var image = UIKit.UIImage.FromImage(cgImage);
            using var data = image.AsJPEG(JpegQuality / 100f);
            return data?.ToArray();
        }, ct);
    }
#elif WINDOWS
    private static async Task<byte[]?> CreatePlatformPreviewAsync(string filePath, int maxDimension, CancellationToken ct)
    {
        using var fileStream = File.OpenRead(filePath);
        using var randomAccessStream = fileStream.AsRandomAccessStream();
        var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(randomAccessStream);

        var scale = Math.Min(1.0, (double)maxDimension / Math.Max(decoder.PixelWidth, decoder.PixelHeight));
        var transform = new Windows.Graphics.Imaging.BitmapTransform
        {
            ScaledWidth = (uint)Math.Max(1, Math.Round(decoder.PixelWidth * scale)),
            ScaledHeight = (uint)Math.Max(1, Math.Round(decoder.PixelHeight * scale)),
            InterpolationMode = Windows.Graphics.Imaging.BitmapInterpolationMode.Fant
        };

        using var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
            Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
            Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied,
            transform,
            Windows.Graphics.Imaging.ExifOrientationMode.RespectExifOrientation,
            Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage);

        using var outputStream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
        var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(
            Windows.Graphics.Imaging.BitmapEncoder.JpegEncoderId, outputStream);
        encoder.SetSoftwareBitmap(softwareBitmap);
        await encoder.FlushAsync();

        outputStream.Seek(0);
        using var resultStream = outputStream.AsStreamForRead();
        using var memoryStream = new MemoryStream();
        await resultStream.CopyToAsync(memoryStream, ct);
        return memoryStream.ToArray();
    }
#else
    private static Task<byte[]?> CreatePlatformPreviewAsync(string filePath, int maxDimension, CancellationToken ct)
        => Task.FromResult<byte[]?>(null);
#endif
}
