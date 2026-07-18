using Heimatplatz.Api.Features.AiListing.Configuration;
using Heimatplatz.Api.Features.AiListing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Shared.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny;

namespace Heimatplatz.Api.Features.AiListing.Services;

/// <summary>
/// Speichert Inserats-Medien im lokalen wwwroot/uploads/listings Verzeichnis
/// (gleiches Muster wie PropertyImageService fuer Bilder). Fotos werden als
/// unveraendertes Original plus Anzeige-Variante abgelegt; das Inserat bindet
/// die Variante ein, das Original bleibt in voller Qualitaet daneben liegen.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class ListingMediaService(
    IWebHostEnvironment environment,
    IOptions<AiListingOptions> options,
    ILogger<ListingMediaService> logger
) : IListingMediaService
{
    private const string UploadFolder = "uploads/listings";
    // Originale bleiben unangetastet - 108-MP-Handyfotos liegen bei 20-40 MB
    private const long MaxImageSize = 60 * 1024 * 1024; // 60 MB

    private static readonly Dictionary<string, string> ImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/heic"] = ".heic"
    };

    private static readonly Dictionary<string, string> VideoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["video/mp4"] = ".mp4",
        ["video/quicktime"] = ".mov",
        ["video/webm"] = ".webm",
        ["video/x-m4v"] = ".m4v"
    };

    /// <inheritdoc />
    public async Task<(List<string> ImageUrls, List<string> VideoUrls)> SaveMediaAsync(
        IReadOnlyList<Base64MediaData> media,
        CancellationToken ct = default)
    {
        var opts = options.Value;
        var uploadPath = Path.Combine(GetWebRoot(), UploadFolder);
        Directory.CreateDirectory(uploadPath);

        var imageUrls = new List<string>();
        var videoUrls = new List<string>();

        foreach (var item in media)
        {
            var isImage = ImageContentTypes.ContainsKey(item.ContentType);
            var isVideo = VideoContentTypes.ContainsKey(item.ContentType);

            if (!isImage && !isVideo)
                throw new ArgumentException(
                    $"Dateityp '{item.ContentType}' fuer '{item.FileName}' nicht erlaubt. " +
                    "Erlaubt: JPEG, PNG, WebP, HEIC (Fotos) sowie MP4, MOV, WebM (Videos).");

            if (string.IsNullOrWhiteSpace(item.Base64Data))
                throw new ArgumentException($"Datei '{item.FileName}' hat keine Daten.");

            var maxSize = isImage ? MaxImageSize : opts.MaxVideoSizeMb * 1024L * 1024L;
            var estimatedSize = item.Base64Data.Length * 3L / 4L;
            if (estimatedSize > maxSize)
                throw new ArgumentException(
                    $"Datei '{item.FileName}' ist zu gross ({estimatedSize / 1024 / 1024} MB). " +
                    $"Maximum: {maxSize / 1024 / 1024} MB.");

            var bytes = Convert.FromBase64String(item.Base64Data);
            var extension = isImage
                ? ImageContentTypes[item.ContentType]
                : VideoContentTypes[item.ContentType];
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadPath, fileName);

            await File.WriteAllBytesAsync(filePath, bytes, ct);

            var url = $"/{UploadFolder}/{fileName}";
            if (isImage)
            {
                // Anzeige-Variante einbinden (klein + auto-orientiert); ohne Variante
                // (kleines Original oder nicht dekodierbar, z.B. HEIC) direkt das Original
                var display = ImageDisplayVariant.TryCreate(bytes);
                if (display != null)
                {
                    var displayName = ImageDisplayVariant.GetDisplayFileName(fileName);
                    await File.WriteAllBytesAsync(Path.Combine(uploadPath, displayName), display, ct);
                    url = $"/{UploadFolder}/{displayName}";
                }

                imageUrls.Add(url);
            }
            else
            {
                videoUrls.Add(url);
            }

            logger.LogInformation("Inserats-Medium gespeichert: {FileName} ({Size} bytes, {ContentType})",
                fileName, bytes.Length, item.ContentType);
        }

        return (imageUrls, videoUrls);
    }

    /// <inheritdoc />
    public string? ResolvePhysicalPath(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        // Absolute URLs auf den Pfad-Anteil reduzieren
        var relativePath = url;
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
            relativePath = absolute.AbsolutePath;

        relativePath = relativePath.TrimStart('/');

        // Nur lokale Upload-Verzeichnisse zulassen
        if (!relativePath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            return null;

        var webRoot = GetWebRoot();
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, relativePath));

        // Path-Traversal verhindern
        var uploadsRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads"));
        if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
            return null;

        return File.Exists(fullPath) ? fullPath : null;
    }

    // WebRootPath ist null, wenn kein wwwroot-Ordner neben dem ContentRoot liegt
    // (z.B. Build-Output ohne Publish) - gleiches Fallback wie ForeclosureImageService
    private string GetWebRoot()
    {
        var webRoot = environment.WebRootPath;
        return string.IsNullOrWhiteSpace(webRoot)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : webRoot;
    }
}
