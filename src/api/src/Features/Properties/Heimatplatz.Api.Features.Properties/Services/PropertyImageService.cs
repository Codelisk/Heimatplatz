using Heimatplatz.Api;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Api.Features.Properties.Services;

/// <summary>
/// Speichert Immobilien-Bilder im lokalen wwwroot/uploads Verzeichnis.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class PropertyImageService(
    IWebHostEnvironment environment,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
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
    public async Task<List<string>> SaveBase64ImagesAsync(IReadOnlyList<Base64ImageData> images, CancellationToken ct = default)
    {
        if (images.Count == 0)
            return [];

        if (images.Count > MaxFiles)
            throw new ArgumentException($"Maximal {MaxFiles} Bilder erlaubt, aber {images.Count} erhalten.");

        var uploadPath = Path.Combine(environment.WebRootPath, UploadFolder);
        Directory.CreateDirectory(uploadPath);

        var urls = new List<string>(images.Count);

        foreach (var image in images)
        {
            ValidateBase64Image(image);

            var bytes = Convert.FromBase64String(image.Base64Data);

            var extension = ContentTypeToExtension.GetValueOrDefault(image.ContentType, ".jpg");
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadPath, fileName);

            await File.WriteAllBytesAsync(filePath, bytes, ct);

            var url = $"/{UploadFolder}/{fileName}";
            urls.Add(url);

            logger.LogInformation("Base64-Bild gespeichert: {FileName} ({Size} bytes)", fileName, bytes.Length);
        }

        return urls;
    }

    /// <inheritdoc />
    public Task DeleteImageAsync(string imageUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return Task.CompletedTask;

        // Eigene Uploads werden teils als absolute URL gespeichert (Upload-Handler geben
        // baseUrl + Pfad zurueck). Absolute URLs nur akzeptieren, wenn der Host der eigene
        // ist - sonst koennte ein Inserat mit gefaelschter externer URL (z.B.
        // https://evil.example/uploads/properties/<fremde-datei>.jpg) beim Loeschen
        // fremde lokale Dateien mitreissen.
        var relativePath = imageUrl;
        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absolute))
        {
            if (!IsOwnHost(absolute))
                return Task.CompletedTask;

            relativePath = absolute.AbsolutePath;
        }

        relativePath = relativePath.TrimStart('/');

        // Nur lokale Upload-Verzeichnisse (KI-erstellte Inserate referenzieren
        // auch uploads/listings-Dateien, nicht nur uploads/properties)
        if (!relativePath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var filePath = Path.GetFullPath(Path.Combine(environment.WebRootPath, relativePath));

        // Path-Traversal verhindern
        var uploadsRoot = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads"));
        if (!filePath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            logger.LogInformation("Bild geloescht: {FilePath}", filePath);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Prueft ob eine absolute URL auf die eigene API zeigt: konfigurierte oeffentliche
    /// Basis-URL (Api:PublicBaseUrl) oder der Host der aktuellen Anfrage.
    /// </summary>
    private bool IsOwnHost(Uri url)
    {
        var configured = configuration["Api:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured)
            && Uri.TryCreate(configured, UriKind.Absolute, out var publicBase)
            && string.Equals(url.Host, publicBase.Host, StringComparison.OrdinalIgnoreCase))
            return true;

        var requestHost = httpContextAccessor.HttpContext?.Request.Host.Host;
        return !string.IsNullOrEmpty(requestHost)
            && string.Equals(url.Host, requestHost, StringComparison.OrdinalIgnoreCase);
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

    private static void ValidateBase64Image(Base64ImageData image)
    {
        if (string.IsNullOrWhiteSpace(image.Base64Data))
            throw new ArgumentException($"Datei '{image.FileName}' hat keine Daten.");

        // Base64-Laenge pruefen (Base64 ist ca. 4/3 der originalen Groesse)
        var estimatedSize = image.Base64Data.Length * 3 / 4;
        if (estimatedSize > MaxFileSize)
            throw new ArgumentException($"Datei '{image.FileName}' ist zu gross ({estimatedSize / 1024 / 1024} MB). Maximum: {MaxFileSize / 1024 / 1024} MB.");

        if (!AllowedContentTypes.Contains(image.ContentType))
            throw new ArgumentException($"Dateityp '{image.ContentType}' fuer '{image.FileName}' nicht erlaubt. Erlaubt: JPEG, PNG, WebP.");
    }
}
