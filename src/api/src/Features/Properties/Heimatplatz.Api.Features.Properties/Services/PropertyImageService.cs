using Heimatplatz.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Properties.Services;

/// <summary>
/// Speichert Immobilien-Bilder im lokalen wwwroot/uploads Verzeichnis.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class PropertyImageService(
    IWebHostEnvironment environment,
    ILogger<PropertyImageService> logger
) : IPropertyImageService
{
    private const string UploadFolder = "uploads/properties";
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
    private const int MaxFiles = 20;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private static readonly Dictionary<string, string> ContentTypeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    /// <inheritdoc />
    public async Task<List<string>> SaveImagesAsync(IReadOnlyList<IFormFile> files, CancellationToken ct = default)
    {
        if (files.Count == 0)
            return [];

        if (files.Count > MaxFiles)
            throw new ArgumentException($"Maximal {MaxFiles} Bilder erlaubt, aber {files.Count} erhalten.");

        var uploadPath = Path.Combine(environment.WebRootPath, UploadFolder);
        Directory.CreateDirectory(uploadPath);

        var urls = new List<string>(files.Count);

        foreach (var file in files)
        {
            ValidateFile(file);

            var extension = ContentTypeToExtension.GetValueOrDefault(file.ContentType, ".jpg");
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadPath, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream, ct);

            var url = $"/{UploadFolder}/{fileName}";
            urls.Add(url);

            logger.LogInformation("Bild gespeichert: {FileName} ({Size} bytes)", fileName, file.Length);
        }

        return urls;
    }

    /// <inheritdoc />
    public Task DeleteImageAsync(string imageUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return Task.CompletedTask;

        // Nur lokale Uploads loeschen (keine externen URLs)
        if (!imageUrl.StartsWith($"/{UploadFolder}/"))
            return Task.CompletedTask;

        var relativePath = imageUrl.TrimStart('/');
        var filePath = Path.Combine(environment.WebRootPath, relativePath);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            logger.LogInformation("Bild geloescht: {FilePath}", filePath);
        }

        return Task.CompletedTask;
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
            throw new ArgumentException($"Datei '{file.FileName}' ist leer.");

        if (file.Length > MaxFileSize)
            throw new ArgumentException($"Datei '{file.FileName}' ist zu gross ({file.Length / 1024 / 1024} MB). Maximum: {MaxFileSize / 1024 / 1024} MB.");

        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new ArgumentException($"Dateityp '{file.ContentType}' fuer '{file.FileName}' nicht erlaubt. Erlaubt: JPEG, PNG, WebP.");
    }
}
