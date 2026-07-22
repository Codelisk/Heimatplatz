using Heimatplatz.Api;
using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Heimatplatz.Api.Shared.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Api.Features.Feedback.Services;

/// <summary>
/// Speichert Feedback-Anhaenge im lokalen wwwroot/uploads/feedback Verzeichnis.
/// Bilder folgen der Inserats-Foto-Pipeline (Original 1:1 + Anzeige-Variante via
/// ImageDisplayVariant), Sprachnachrichten werden unveraendert abgelegt.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class FeedbackAttachmentService(
    IWebHostEnvironment environment,
    ILogger<FeedbackAttachmentService> logger
) : IFeedbackAttachmentService
{
    private const string UploadFolder = "uploads/feedback";

    // Originale bleiben unangetastet - Handyfotos liegen bei 20-40 MB
    private const long MaxImageSize = 60 * 1024 * 1024; // 60 MB
    // WAV 16 kHz/16 bit mono = ~2 MB/Minute; 25 MB reichen fuer sehr lange Aufnahmen
    private const long MaxAudioSize = 25 * 1024 * 1024; // 25 MB

    private static readonly Dictionary<string, string> ImageContentTypeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private static readonly Dictionary<string, string> AudioContentTypeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["audio/wav"] = ".wav",
        ["audio/x-wav"] = ".wav",
        ["audio/wave"] = ".wav",
        ["audio/mp4"] = ".m4a",
        ["audio/m4a"] = ".m4a",
        ["audio/x-m4a"] = ".m4a",
        ["audio/aac"] = ".aac",
        ["audio/mpeg"] = ".mp3"
    };

    private static readonly Dictionary<string, (FeedbackAttachmentKind Kind, string ContentType)> ExtensionToMeta = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = (FeedbackAttachmentKind.Image, "image/jpeg"),
        [".jpeg"] = (FeedbackAttachmentKind.Image, "image/jpeg"),
        [".png"] = (FeedbackAttachmentKind.Image, "image/png"),
        [".webp"] = (FeedbackAttachmentKind.Image, "image/webp"),
        [".wav"] = (FeedbackAttachmentKind.Audio, "audio/wav"),
        [".m4a"] = (FeedbackAttachmentKind.Audio, "audio/mp4"),
        [".aac"] = (FeedbackAttachmentKind.Audio, "audio/aac"),
        [".mp3"] = (FeedbackAttachmentKind.Audio, "audio/mpeg")
    };

    /// <inheritdoc />
    public async Task<SavedFeedbackAttachment> SaveBase64Async(string fileName, string contentType, string base64Data, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(base64Data))
            throw new ArgumentException($"Datei '{fileName}' hat keine Daten.");

        var isImage = ImageContentTypeToExtension.ContainsKey(contentType);
        var isAudio = AudioContentTypeToExtension.ContainsKey(contentType);
        if (!isImage && !isAudio)
            throw new ArgumentException(
                $"Dateityp '{contentType}' fuer '{fileName}' nicht erlaubt. Erlaubt: JPEG, PNG, WebP, WAV, M4A, AAC, MP3.");

        // Base64-Laenge pruefen (Base64 ist ca. 4/3 der originalen Groesse)
        var maxSize = isImage ? MaxImageSize : MaxAudioSize;
        var estimatedSize = (long)base64Data.Length * 3 / 4;
        if (estimatedSize > maxSize)
            throw new ArgumentException(
                $"Datei '{fileName}' ist zu gross ({estimatedSize / 1024 / 1024} MB). Maximum: {maxSize / 1024 / 1024} MB.");

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64Data);
        }
        catch (FormatException)
        {
            throw new ArgumentException($"Datei '{fileName}' enthaelt kein gueltiges Base64.");
        }

        var uploadPath = Path.Combine(GetWebRoot(), UploadFolder);
        Directory.CreateDirectory(uploadPath);

        var extension = isImage
            ? ImageContentTypeToExtension[contentType]
            : AudioContentTypeToExtension[contentType];
        var storedName = $"{Guid.NewGuid()}{extension}";

        await File.WriteAllBytesAsync(Path.Combine(uploadPath, storedName), bytes, ct);
        var url = $"/{UploadFolder}/{storedName}";

        if (isImage)
        {
            var display = ImageDisplayVariant.TryCreate(bytes);
            if (display != null)
            {
                var displayName = ImageDisplayVariant.GetDisplayFileName(storedName);
                await File.WriteAllBytesAsync(Path.Combine(uploadPath, displayName), display, ct);
                url = $"/{UploadFolder}/{displayName}";
            }
        }

        logger.LogInformation("Feedback-Anhang gespeichert: {FileName} ({Size} bytes, {Kind})",
            storedName, bytes.Length, isImage ? "Bild" : "Audio");

        return new SavedFeedbackAttachment(url, isImage ? FeedbackAttachmentKind.Image : FeedbackAttachmentKind.Audio, bytes.Length);
    }

    /// <inheritdoc />
    public ResolvedFeedbackAttachment Resolve(string url)
    {
        var filePath = GetSafeFilePath(url, out var relativeUrl)
            ?? throw new ArgumentException($"Anhang-URL '{url}' ist ungueltig (erwartet wird eine Upload-URL aus /uploads/feedback).");

        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new ArgumentException($"Anhang '{url}' wurde nicht gefunden - bitte erneut hochladen.");

        var extension = fileInfo.Extension;
        if (!ExtensionToMeta.TryGetValue(extension, out var meta))
            throw new ArgumentException($"Anhang '{url}' hat einen unbekannten Dateityp.");

        return new ResolvedFeedbackAttachment(relativeUrl, meta.Kind, meta.ContentType, fileInfo.Length);
    }

    /// <inheritdoc />
    public Task DeleteAttachmentFilesAsync(IEnumerable<string> urls, CancellationToken ct = default)
    {
        foreach (var url in urls)
        {
            var filePath = GetSafeFilePath(url, out _);
            if (filePath == null)
                continue;

            // Original und Anzeige-Variante teilen sich den GUID-Stamm - beide entfernen
            var directory = Path.GetDirectoryName(filePath);
            var stem = ImageDisplayVariant.GetFileStem(Path.GetFileName(filePath));
            if (directory == null || string.IsNullOrWhiteSpace(stem) || !Directory.Exists(directory))
                continue;

            foreach (var siblingPath in Directory.EnumerateFiles(directory, $"{stem}.*"))
            {
                File.Delete(siblingPath);
                logger.LogInformation("Feedback-Anhang geloescht: {FilePath}", siblingPath);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Uebersetzt eine Anhang-URL (relativ oder absolut) in einen sicheren Dateipfad
    /// unterhalb von uploads/feedback. Null bei fremden Pfaden oder Path-Traversal.
    /// Fremde Hosts sind unkritisch, weil ausschliesslich der Pfad verwendet wird.
    /// </summary>
    private string? GetSafeFilePath(string url, out string relativeUrl)
    {
        relativeUrl = url;
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
            relativeUrl = absolute.AbsolutePath;

        if (!relativeUrl.StartsWith($"/{UploadFolder}/", StringComparison.OrdinalIgnoreCase))
            return null;

        var webRoot = GetWebRoot();
        var filePath = Path.GetFullPath(Path.Combine(webRoot, relativeUrl.TrimStart('/')));

        // Path-Traversal verhindern
        var uploadsRoot = Path.GetFullPath(Path.Combine(webRoot, UploadFolder));
        if (!filePath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
            return null;

        return filePath;
    }

    // WebRootPath ist null, wenn kein wwwroot-Ordner neben dem ContentRoot liegt
    // (z.B. Build-Output ohne Publish) - gleiches Fallback wie PropertyImageService
    private string GetWebRoot()
    {
        var webRoot = environment.WebRootPath;
        return string.IsNullOrWhiteSpace(webRoot)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : webRoot;
    }
}
