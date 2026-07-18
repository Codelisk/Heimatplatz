using SkiaSharp;

namespace Heimatplatz.Api.Shared.Media;

/// <summary>
/// Erzeugt anzeigefertige JPEG-Varianten von Upload-Fotos. Das Original bleibt
/// immer unveraendert in voller Qualitaet gespeichert; nur die Variante wird in
/// Inseraten eingebunden (Bandbreite + Android-Canvas-Limit bei 100+-MP-Fotos).
///
/// Namenskonvention: Original "{guid}{ext}", Variante "{guid}.display.jpg".
/// Loeschroutinen entfernen ueber den gemeinsamen GUID-Stamm beide Dateien.
/// </summary>
public static class ImageDisplayVariant
{
    /// <summary>Dateisuffix der Anzeige-Variante (vor der Extension).</summary>
    public const string FileSuffix = ".display";

    /// <summary>Maximale Kantenlaenge der Anzeige-Variante in Pixel.</summary>
    public const int MaxDimension = 2560;

    private const int JpegQuality = 85;

    /// <summary>
    /// Erzeugt die Anzeige-Variante (auto-orientiert nach EXIF, max. 2560 px).
    /// Liefert null, wenn keine Variante noetig ist (Original klein genug und
    /// richtig orientiert) oder das Format nicht dekodierbar ist (z.B. HEIC) -
    /// in beiden Faellen soll das Original direkt eingebunden werden.
    /// </summary>
    public static byte[]? TryCreate(byte[] originalBytes)
    {
        using var data = SKData.CreateCopy(originalBytes);
        using var codec = SKCodec.Create(data);
        if (codec == null)
            return null;

        var origin = codec.EncodedOrigin;
        var info = codec.Info;
        var needsResize = Math.Max(info.Width, info.Height) > MaxDimension;
        var needsRotation = origin != SKEncodedOrigin.TopLeft;
        if (!needsResize && !needsRotation)
            return null;

        // Skaliert dekodieren: JPEG kann nativ in 1/8-Schritten verkleinern,
        // damit landet ein 100-MP-Foto nie komplett im Speicher
        var targetScale = needsResize
            ? (float)MaxDimension / Math.Max(info.Width, info.Height)
            : 1f;
        var scaledSize = codec.GetScaledDimensions(targetScale);
        var decodeInfo = new SKImageInfo(scaledSize.Width, scaledSize.Height);

        using var decoded = SKBitmap.Decode(codec, decodeInfo);
        if (decoded == null)
            return null;

        var bitmap = decoded;
        try
        {
            // Codec-Skalierung ist grob (JPEG: 1/8-Raster, andere Formate gar nicht) -
            // exakt auf die Zielkante nachziehen
            if (Math.Max(bitmap.Width, bitmap.Height) > MaxDimension)
            {
                var scale = (float)MaxDimension / Math.Max(bitmap.Width, bitmap.Height);
                var resizeInfo = new SKImageInfo(
                    Math.Max(1, (int)Math.Round(bitmap.Width * scale)),
                    Math.Max(1, (int)Math.Round(bitmap.Height * scale)));
                var resized = bitmap.Resize(resizeInfo, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
                if (resized == null)
                    return null;

                if (!ReferenceEquals(bitmap, decoded))
                    bitmap.Dispose();
                bitmap = resized;
            }

            if (needsRotation)
            {
                var oriented = ApplyOrigin(bitmap, origin);
                if (!ReferenceEquals(bitmap, decoded))
                    bitmap.Dispose();
                bitmap = oriented;
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
            return encoded?.ToArray();
        }
        finally
        {
            if (!ReferenceEquals(bitmap, decoded))
                bitmap.Dispose();
        }
    }

    /// <summary>
    /// Liefert die URL der zugehoerigen Anzeige-Variante zu einer Original-URL
    /// (bzw. unveraendert, wenn schon eine Varianten-URL uebergeben wurde).
    /// </summary>
    public static string GetDisplayFileName(string originalFileName)
    {
        var stem = Path.GetFileNameWithoutExtension(originalFileName);
        return $"{stem}{FileSuffix}.jpg";
    }

    /// <summary>
    /// Gemeinsamer Dateistamm von Original und Variante, z.B. "3f2a..." fuer
    /// "3f2a....jpg" und "3f2a....display.jpg" - Basis fuer das Mitloeschen.
    /// </summary>
    public static string GetFileStem(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        return stem.EndsWith(FileSuffix, StringComparison.OrdinalIgnoreCase)
            ? stem[..^FileSuffix.Length]
            : stem;
    }

    /// <summary>
    /// Wendet die EXIF-Orientierung auf die Bitmap an (alle 8 Faelle inkl.
    /// gespiegelter Varianten), gleiches Canvas-Muster wie ForeclosureImageService.
    /// </summary>
    private static SKBitmap ApplyOrigin(SKBitmap source, SKEncodedOrigin origin)
    {
        var swapsDimensions = origin is
            SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or
            SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

        var destination = new SKBitmap(
            swapsDimensions ? source.Height : source.Width,
            swapsDimensions ? source.Width : source.Height,
            source.ColorType,
            source.AlphaType);

        using var canvas = new SKCanvas(destination);
        canvas.Clear(SKColors.Transparent);

        switch (origin)
        {
            case SKEncodedOrigin.TopRight: // horizontal gespiegelt
                canvas.Scale(-1, 1, source.Width / 2f, 0);
                break;
            case SKEncodedOrigin.BottomRight: // 180 Grad
                canvas.Translate(destination.Width, destination.Height);
                canvas.RotateDegrees(180);
                break;
            case SKEncodedOrigin.BottomLeft: // vertikal gespiegelt
                canvas.Scale(1, -1, 0, source.Height / 2f);
                break;
            case SKEncodedOrigin.LeftTop: // 90 Grad im UZS + gespiegelt
                canvas.RotateDegrees(90);
                canvas.Scale(1, -1, source.Width / 2f, 0);
                break;
            case SKEncodedOrigin.RightTop: // 90 Grad im UZS
                canvas.Translate(destination.Width, 0);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightBottom: // 90 Grad gegen UZS + gespiegelt
                canvas.Translate(destination.Width, 0);
                canvas.RotateDegrees(90);
                canvas.Scale(-1, 1, source.Width / 2f, 0);
                break;
            case SKEncodedOrigin.LeftBottom: // 90 Grad gegen UZS
                canvas.Translate(0, destination.Height);
                canvas.RotateDegrees(-90);
                break;
        }

        canvas.DrawBitmap(source, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest), paint: null);
        canvas.Flush();
        return destination;
    }
}
