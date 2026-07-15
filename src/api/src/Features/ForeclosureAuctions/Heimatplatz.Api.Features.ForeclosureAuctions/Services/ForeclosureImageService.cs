using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Heimatplatz.Api.Features.ForeclosureAuctions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;
using UglyToad.PdfPig;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Services;

/// <summary>
/// Bereitet hochwertige Bilder fuer Zwangsversteigerungen auf. Direkte
/// Edikt-Fotos bleiben externe Original-URLs. Nur wenn diese technisch nicht
/// brauchbar sind, werden Bilder einmalig aus dem Langgutachten extrahiert und
/// unter wwwroot/uploads/foreclosures persistent zwischengespeichert.
/// </summary>
public sealed class ForeclosureImageService(
    IHttpClientFactory httpClientFactory,
    IWebHostEnvironment environment,
    IOptions<ScrapingOptions> options,
    ILogger<ForeclosureImageService> logger
) : IForeclosureImageService
{
    public const string HttpClientName = "ForeclosureDocuments";
    private const string UploadFolder = "uploads/foreclosures";
    private const int MaxRedirects = 5;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record ImageManifest(List<string> Urls);

    public async Task<List<string>> PrepareImageUrlsAsync(EdiktDetail detail, CancellationToken ct = default)
    {
        var directImages = ForeclosureImageSelector.SelectDirectImages(
            detail.ImageCandidates,
            maxCount: options.Value.MaxImagesPerAuction);
        if (directImages.Count > 0)
            return directImages.Select(image => image.Url).ToList();

        if (options.Value.EnablePdfImageFallback && !string.IsNullOrWhiteSpace(detail.LongAppraisalUrl))
        {
            try
            {
                var extracted = await GetOrExtractPdfImagesAsync(
                    detail.ExternalId,
                    detail.LongAppraisalUrl,
                    ct);
                if (extracted.Count > 0)
                {
                    logger.LogInformation(
                        "{Count} hochwertige Bilder fuer Edikt {ExternalId} aus dem Langgutachten verwendet",
                        extracted.Count,
                        detail.ExternalId);
                    return extracted;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Ein fehlerhaftes oder ungewoehnlich kodiertes Gutachten darf
                // den gesamten Edikte-Sync nicht abbrechen.
                logger.LogWarning(
                    ex,
                    "PDF-Bildextraktion fuer Edikt {ExternalId} fehlgeschlagen",
                    detail.ExternalId);
            }
        }

        // Moderate Originale sind immer noch besser als 225px-Kontaktboegen.
        // Unterhalb 480x300 bleibt die Galerie bewusst leer und zeigt ihren
        // neutralen Platzhalter, statt Pixelartefakte gross aufzuziehen.
        return ForeclosureImageSelector.SelectDirectImages(
                detail.ImageCandidates,
                requirePrimaryQuality: false,
                maxCount: options.Value.MaxImagesPerAuction)
            .Select(image => image.Url)
            .ToList();
    }

    private async Task<List<string>> GetOrExtractPdfImagesAsync(
        string externalId,
        string pdfUrl,
        CancellationToken ct)
    {
        if (!Uri.TryCreate(pdfUrl, UriKind.Absolute, out var pdfUri) || !IsTrustedJustizUri(pdfUri))
        {
            logger.LogWarning("Nicht erlaubte Langgutachten-URL fuer Edikt {ExternalId}: {Url}", externalId, pdfUrl);
            return [];
        }

        var webRoot = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(environment.ContentRootPath, "wwwroot");

        var safeExternalId = string.Concat(externalId.Where(char.IsAsciiHexDigit)).ToLowerInvariant();
        if (safeExternalId.Length == 0)
            return [];

        var relativeDirectory = $"{UploadFolder}/{safeExternalId}";
        var outputDirectory = Path.Combine(webRoot, UploadFolder, safeExternalId);
        Directory.CreateDirectory(outputDirectory);

        var sourceKey = ComputeHash(Encoding.UTF8.GetBytes(pdfUri.AbsoluteUri))[..20];
        var manifestPath = Path.Combine(outputDirectory, $"manifest-{sourceKey}.json");
        var cached = await ReadValidManifestAsync(manifestPath, webRoot, ct);
        if (cached is not null)
            return cached;

        var pdfBytes = await DownloadPdfAsync(pdfUri, ct);
        var candidates = ExtractPdfImageCandidates(pdfBytes);
        var selected = ForeclosureImageSelector.SelectPdfImages(
            candidates,
            options.Value.MaxImagesPerAuction);

        var urls = new List<string>(selected.Count);
        foreach (var image in selected)
        {
            var hash = ComputeHash(image.Bytes)[..24];
            var fileName = $"{hash}{image.Extension}";
            var filePath = Path.Combine(outputDirectory, fileName);
            if (!File.Exists(filePath))
                await File.WriteAllBytesAsync(filePath, image.Bytes, ct);

            urls.Add($"/{relativeDirectory}/{fileName}");
        }

        // Auch ein leeres Ergebnis wird gecacht. Sonst wuerden ungeeignete
        // Gutachten bei jedem periodischen Sync erneut geladen und analysiert.
        await WriteManifestAtomicallyAsync(manifestPath, urls, ct);

        return urls;
    }

    internal List<PdfImageCandidate> ExtractPdfImageCandidates(byte[] pdfBytes)
    {
        var candidates = new List<PdfImageCandidate>();
        var seenHashes = new HashSet<string>(StringComparer.Ordinal);

        using var document = PdfDocument.Open(pdfBytes, new ParsingOptions
        {
            UseLenientParsing = true,
            SkipMissingFonts = true
        });

        foreach (var page in document.GetPages())
        {
            var imageOrder = 0;
            // PDF-Objektreihenfolge ist nicht zwingend die sichtbare Lesereihenfolge.
            // Von oben nach unten sortiert landet die typische Aussenansicht zuerst.
            var pageImages = page.GetImages()
                .OrderByDescending(image => image.BoundingBox.Centroid.Y)
                .ThenBy(image => image.BoundingBox.Centroid.X);

            foreach (var pdfImage in pageImages)
            {
                try
                {
                    byte[] bytes;
                    string extension;
                    var rawBytes = pdfImage.RawBytes.ToArray();
                    if (IsJpeg(rawBytes))
                    {
                        bytes = rawBytes;
                        extension = ".jpg";
                    }
                    else if (pdfImage.TryGetPng(out var pngBytes))
                    {
                        bytes = pngBytes;
                        extension = ".png";
                    }
                    else
                    {
                        imageOrder++;
                        continue;
                    }

                    var normalized = ApplyPdfRotation(
                        bytes,
                        extension,
                        pdfImage.WidthInSamples,
                        pdfImage.HeightInSamples,
                        pdfImage.BoundingBox.Rotation);
                    bytes = normalized.Bytes;
                    extension = normalized.Extension;

                    var hash = ComputeHash(bytes);
                    if (!seenHashes.Add(hash))
                    {
                        imageOrder++;
                        continue;
                    }

                    candidates.Add(new PdfImageCandidate(
                        page.Number,
                        imageOrder++,
                        page.Text,
                        normalized.Width,
                        normalized.Height,
                        bytes,
                        extension));
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Eingebettetes PDF-Bild auf Seite {Page} konnte nicht dekodiert werden", page.Number);
                    imageOrder++;
                }
            }
        }

        return candidates;
    }

    /// <summary>
    /// Eingebettete JPEGs enthalten die PDF-Platzierungsrotation nicht selbst.
    /// Ohne diese Korrektur erscheinen manche Gutachten-Fotos seitlich gedreht.
    /// Positive PDF-Winkel sind gegen den Uhrzeigersinn definiert.
    /// </summary>
    internal static (byte[] Bytes, int Width, int Height, string Extension) ApplyPdfRotation(
        byte[] bytes,
        string extension,
        int width,
        int height,
        double rotationDegrees)
    {
        var snappedRotation = (int)Math.Round(rotationDegrees / 90d) * 90;
        if (Math.Abs(rotationDegrees - snappedRotation) > 1d)
            return (bytes, width, height, extension);

        var normalizedRotation = ((snappedRotation % 360) + 360) % 360;
        if (normalizedRotation == 0)
            return (bytes, width, height, extension);

        using var source = SKBitmap.Decode(bytes)
            ?? throw new InvalidOperationException("Eingebettetes PDF-Bild konnte nicht dekodiert werden");
        var swapsDimensions = normalizedRotation is 90 or 270;
        using var destination = new SKBitmap(
            swapsDimensions ? source.Height : source.Width,
            swapsDimensions ? source.Width : source.Height,
            source.ColorType,
            source.AlphaType);
        using var canvas = new SKCanvas(destination);
        canvas.Clear(SKColors.Transparent);

        switch (normalizedRotation)
        {
            case 90:
                canvas.Translate(0, destination.Height);
                canvas.RotateDegrees(-90);
                break;
            case 180:
                canvas.Translate(destination.Width, destination.Height);
                canvas.RotateDegrees(180);
                break;
            case 270:
                canvas.Translate(destination.Width, 0);
                canvas.RotateDegrees(90);
                break;
        }

        canvas.DrawBitmap(
            source,
            0,
            0,
            new SKSamplingOptions(SKFilterMode.Nearest),
            paint: null);
        canvas.Flush();

        using var image = SKImage.FromBitmap(destination);
        var format = string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
            ? SKEncodedImageFormat.Png
            : SKEncodedImageFormat.Jpeg;
        using var encoded = image.Encode(format, format == SKEncodedImageFormat.Jpeg ? 90 : 100)
            ?? throw new InvalidOperationException("Gedrehtes PDF-Bild konnte nicht kodiert werden");

        return (
            encoded.ToArray(),
            destination.Width,
            destination.Height,
            format == SKEncodedImageFormat.Png ? ".png" : ".jpg");
    }

    private async Task<byte[]> DownloadPdfAsync(Uri initialUri, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        var currentUri = initialUri;

        for (var redirectCount = 0; redirectCount <= MaxRedirects; redirectCount++)
        {
            using var response = await client.GetAsync(currentUri, HttpCompletionOption.ResponseHeadersRead, ct);
            if (IsRedirect(response.StatusCode))
            {
                var location = response.Headers.Location
                    ?? throw new InvalidOperationException("Langgutachten-Redirect ohne Ziel-URL");
                currentUri = location.IsAbsoluteUri ? location : new Uri(currentUri, location);
                if (!IsTrustedJustizUri(currentUri))
                    throw new InvalidOperationException("Langgutachten-Redirect auf nicht erlaubten Host");
                continue;
            }

            response.EnsureSuccessStatusCode();
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (!string.Equals(mediaType, "application/pdf", StringComparison.OrdinalIgnoreCase)
                && !currentUri.AbsolutePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Langgutachten hat unerwarteten Content-Type: {mediaType}");
            }

            var maxBytes = options.Value.MaxAppraisalPdfBytes;
            if (response.Content.Headers.ContentLength is > 0
                && response.Content.Headers.ContentLength > maxBytes)
            {
                throw new InvalidOperationException("Langgutachten ueberschreitet das konfigurierte Groessenlimit");
            }

            await using var source = await response.Content.ReadAsStreamAsync(ct);
            await using var target = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while ((read = await source.ReadAsync(buffer, ct)) > 0)
            {
                if (target.Length + read > maxBytes)
                    throw new InvalidOperationException("Langgutachten ueberschreitet das konfigurierte Groessenlimit");

                await target.WriteAsync(buffer.AsMemory(0, read), ct);
            }

            return target.ToArray();
        }

        throw new InvalidOperationException("Zu viele Redirects beim Laden des Langgutachtens");
    }

    private static async Task<List<string>?> ReadValidManifestAsync(
        string manifestPath,
        string webRoot,
        CancellationToken ct)
    {
        if (!File.Exists(manifestPath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(manifestPath, ct);
            var manifest = JsonSerializer.Deserialize<ImageManifest>(json, JsonOptions);
            if (manifest is null)
                return null;

            return manifest.Urls.All(url => LocalUrlExists(webRoot, url))
                ? manifest.Urls
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task WriteManifestAtomicallyAsync(
        string manifestPath,
        List<string> urls,
        CancellationToken ct)
    {
        var temporaryPath = $"{manifestPath}.{Guid.NewGuid():N}.tmp";
        var json = JsonSerializer.Serialize(new ImageManifest(urls), JsonOptions);
        await File.WriteAllTextAsync(temporaryPath, json, ct);
        File.Move(temporaryPath, manifestPath, overwrite: true);
    }

    private static bool LocalUrlExists(string webRoot, string url)
    {
        if (!url.StartsWith($"/{UploadFolder}/", StringComparison.Ordinal))
            return false;

        var relativePath = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, relativePath));
        var uploadsRoot = Path.GetFullPath(Path.Combine(webRoot, UploadFolder));
        return fullPath.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && File.Exists(fullPath);
    }

    private static bool IsJpeg(byte[] bytes) =>
        bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff;

    private static bool IsTrustedJustizUri(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps
        && uri.IsDefaultPort
        && (string.Equals(uri.Host, "justiz.gv.at", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".justiz.gv.at", StringComparison.OrdinalIgnoreCase));

    private static bool IsRedirect(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.MovedPermanently or
        HttpStatusCode.Found or
        HttpStatusCode.SeeOther or
        HttpStatusCode.TemporaryRedirect or
        HttpStatusCode.PermanentRedirect;

    private static string ComputeHash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
