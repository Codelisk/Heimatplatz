using Heimatplatz.Api;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Shared.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Api.Features.Properties.Services;

/// <summary>
/// Speichert Immobilien-Bilder im lokalen wwwroot/uploads Verzeichnis.
/// Fotos werden als unveraendertes Original plus Anzeige-Variante abgelegt;
/// das Inserat bindet die Variante ein, das Original bleibt in voller
/// Qualitaet daneben liegen.
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
    // Originale bleiben unangetastet - 108-MP-Handyfotos liegen bei 20-40 MB
    private const long MaxFileSize = 60 * 1024 * 1024; // 60 MB
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

        var uploadPath = Path.Combine(GetWebRoot(), UploadFolder);
        Directory.CreateDirectory(uploadPath);

        var urls = new List<string>(files.Count);

        foreach (var file in files)
        {
            ValidateFile(file);

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, ct);

            urls.Add(await SaveImageBytesAsync(uploadPath, memoryStream.ToArray(), file.ContentType, ct));
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

        var uploadPath = Path.Combine(GetWebRoot(), UploadFolder);
        Directory.CreateDirectory(uploadPath);

        var urls = new List<string>(images.Count);

        foreach (var image in images)
        {
            ValidateBase64Image(image);

            var bytes = Convert.FromBase64String(image.Base64Data);
            urls.Add(await SaveImageBytesAsync(uploadPath, bytes, image.ContentType, ct));
        }

        return urls;
    }

    /// <summary>
    /// Speichert das unveraenderte Original und erzeugt daneben die Anzeige-Variante.
    /// Zurueckgegeben wird die Varianten-URL (bzw. das Original, wenn keine noetig ist).
    /// </summary>
    private async Task<string> SaveImageBytesAsync(string uploadPath, byte[] bytes, string contentType, CancellationToken ct)
    {
        var extension = ContentTypeToExtension.GetValueOrDefault(contentType, ".jpg");
        var fileName = $"{Guid.NewGuid()}{extension}";

        await File.WriteAllBytesAsync(Path.Combine(uploadPath, fileName), bytes, ct);

        var url = $"/{UploadFolder}/{fileName}";
        var display = ImageDisplayVariant.TryCreate(bytes);
        if (display != null)
        {
            var displayName = ImageDisplayVariant.GetDisplayFileName(fileName);
            await File.WriteAllBytesAsync(Path.Combine(uploadPath, displayName), display, ct);
            url = $"/{UploadFolder}/{displayName}";
        }

        logger.LogInformation("Bild gespeichert: {FileName} ({Size} bytes, Variante: {HasDisplay})",
            fileName, bytes.Length, display != null);

        return url;
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

        var webRoot = GetWebRoot();
        var filePath = Path.GetFullPath(Path.Combine(webRoot, relativePath));

        // Path-Traversal verhindern
        var uploadsRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads"));
        if (!filePath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        // Original und Anzeige-Variante teilen sich den GUID-Stamm - beide entfernen,
        // egal welche der beiden URLs im Inserat referenziert war
        var directory = Path.GetDirectoryName(filePath);
        var stem = ImageDisplayVariant.GetFileStem(Path.GetFileName(filePath));
        if (directory != null && !string.IsNullOrWhiteSpace(stem) && Directory.Exists(directory))
        {
            foreach (var siblingPath in Directory.EnumerateFiles(directory, $"{stem}.*"))
            {
                File.Delete(siblingPath);
                logger.LogInformation("Bild geloescht: {FilePath}", siblingPath);
            }
        }

        return Task.CompletedTask;
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
