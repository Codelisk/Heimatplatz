using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Heimatplatz.Api.Features.OpenImmoImport.Configuration;
using Heimatplatz.Api.Features.OpenImmoImport.Models;
using Heimatplatz.Api.Shared.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.OpenImmoImport.Services;

public interface IOpenImmoImageService
{
    /// <summary>
    /// Materialisiert die Bild-Anhaenge eines Objekts nach
    /// wwwroot/uploads/openimmo/{feedKey}/{safeSourceId}/ (Original + Display-Variante)
    /// und liefert die lokalen URLs fuer Property.ImageUrls. Bereits bekannte Bilder
    /// (manifest.json) werden wiederverwendet, verschwundene aufgeraeumt. Einzelne
    /// Bild-Fehler degradieren zu weniger Bildern - die Methode wirft nie (ausser Cancel).
    /// </summary>
    Task<List<string>> MaterializeAsync(
        OpenImmoFeedOptions feed,
        OpenImmoListing listing,
        IOpenImmoZipAccessor? zip,
        CancellationToken ct = default);

    /// <summary>Loescht den kompletten Bild-Ordner eines Objekts (Delete-Pass des Syncs).</summary>
    void DeleteListingImages(string feedKey, string sourceId);
}

/// <summary>
/// Bild-Pipeline des OpenImmo-Imports nach dem Muster von ForeclosureImageService
/// (Hash-Dateinamen, manifest.json mit atomarem Write, Host-Allowlist, Groessencaps)
/// kombiniert mit der PropertyImageService-Konvention (Original {key}{ext} +
/// {key}.display.jpg via ImageDisplayVariant; eingebettet wird die Display-URL).
/// </summary>
public sealed class OpenImmoImageService(
    IHttpClientFactory httpClientFactory,
    IWebHostEnvironment environment,
    IOptions<OpenImmoImportOptions> options,
    ILogger<OpenImmoImageService> logger
) : IOpenImmoImageService
{
    public const string HttpClientName = "OpenImmoImages";
    private const string UploadFolder = "uploads/openimmo";
    private const int MaxRedirects = 5;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record ManifestEntry(string Key, string Url);
    private sealed record ImageManifest(List<ManifestEntry> Entries);

    public async Task<List<string>> MaterializeAsync(
        OpenImmoFeedOptions feed,
        OpenImmoListing listing,
        IOpenImmoZipAccessor? zip,
        CancellationToken ct = default)
    {
        var safeSourceId = BuildSafeSourceId(listing.SourceId);
        var relativeDirectory = $"{UploadFolder}/{SafeFeedKey(feed.Key)}/{safeSourceId}";
        var outputDirectory = Path.Combine(GetWebRoot(), relativeDirectory.Replace('/', Path.DirectorySeparatorChar));

        var attachments = listing.Attachments.Take(options.Value.MaxImagesPerProperty).ToList();
        if (attachments.Count == 0)
        {
            // Kein Bild mehr im Feed: eventuelle Altbilder aufraeumen
            DeleteListingImages(feed.Key, listing.SourceId);
            return [];
        }

        Directory.CreateDirectory(outputDirectory);

        var manifestPath = Path.Combine(outputDirectory, "manifest.json");
        var manifest = await ReadManifestAsync(manifestPath, ct);

        var urls = new List<string>();
        var keptKeys = new HashSet<string>(StringComparer.Ordinal);
        var newEntries = new List<ManifestEntry>();

        foreach (var attachment in attachments)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var entry = await MaterializeAttachmentAsync(
                    attachment, feed, zip, manifest, outputDirectory, relativeDirectory, ct);
                if (entry == null || !keptKeys.Add(entry.Key))
                    continue;

                newEntries.Add(entry);
                urls.Add(entry.Url);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Bild-Anhang fuer Objekt {SourceId} (Feed {FeedKey}) konnte nicht uebernommen werden",
                    listing.SourceId, feed.Key);
            }
        }

        CleanupOrphans(outputDirectory, keptKeys);
        await WriteManifestAtomicallyAsync(manifestPath, newEntries, ct);

        return urls;
    }

    public void DeleteListingImages(string feedKey, string sourceId)
    {
        var directory = Path.Combine(
            GetWebRoot(), UploadFolder, SafeFeedKey(feedKey), BuildSafeSourceId(sourceId));
        if (Directory.Exists(directory))
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException ex)
            {
                logger.LogWarning(ex, "Bild-Ordner {Directory} konnte nicht geloescht werden", directory);
            }
        }
    }

    private async Task<ManifestEntry?> MaterializeAttachmentAsync(
        OpenImmoAttachment attachment,
        OpenImmoFeedOptions feed,
        IOpenImmoZipAccessor? zip,
        ImageManifest? manifest,
        string outputDirectory,
        string relativeDirectory,
        CancellationToken ct)
    {
        // EXTERN: Identity-Key aus der URL - erspart den Re-Download bekannter Bilder
        if (attachment.Mode == OpenImmoAttachmentMode.ExternalUrl)
        {
            var urlKey = ComputeHash(Encoding.UTF8.GetBytes(attachment.Location!))[..20];
            var cached = TryReuse(manifest, urlKey, outputDirectory);
            if (cached != null)
                return cached;

            var downloaded = await DownloadImageAsync(attachment.Location!, feed.AllowedImageHosts, ct);
            return downloaded == null
                ? null
                : StoreImage(urlKey, downloaded, outputDirectory, relativeDirectory);
        }

        // Base64/ZIP: Inhalt muss ohnehin gelesen werden, Key aus dem Inhalt
        var bytes = attachment.Mode switch
        {
            OpenImmoAttachmentMode.Base64 => DecodeBase64(attachment.Base64Content!),
            OpenImmoAttachmentMode.ZipEntry => zip?.ReadEntry(attachment.Location!, options.Value.MaxAttachmentBytes),
            _ => null
        };
        if (bytes == null)
            return null;

        var contentKey = ComputeHash(bytes)[..20];
        return TryReuse(manifest, contentKey, outputDirectory)
            ?? StoreImage(contentKey, bytes, outputDirectory, relativeDirectory);
    }

    /// <summary>Manifest-Eintrag nur wiederverwenden, wenn die Datei wirklich noch existiert.</summary>
    private static ManifestEntry? TryReuse(ImageManifest? manifest, string key, string outputDirectory)
    {
        var entry = manifest?.Entries.FirstOrDefault(e => e.Key == key);
        if (entry == null)
            return null;

        var fileName = Path.GetFileName(entry.Url);
        return File.Exists(Path.Combine(outputDirectory, fileName)) ? entry : null;
    }

    /// <summary>
    /// Original {key}{ext} + Display-Variante {key}.display.jpg; eingebettet wird die
    /// Variante, sonst das Original (exakt die PropertyImageService-Konvention, damit
    /// /api/images/local-Thumbnails und Loeschen-per-Stem unveraendert funktionieren).
    /// </summary>
    private ManifestEntry? StoreImage(string key, byte[] bytes, string outputDirectory, string relativeDirectory)
    {
        var extension = DetectImageExtension(bytes);
        if (extension == null)
        {
            logger.LogDebug("Anhang {Key} ist kein unterstuetztes Bildformat - uebersprungen", key);
            return null;
        }

        var originalName = $"{key}{extension}";
        var originalPath = Path.Combine(outputDirectory, originalName);
        if (!File.Exists(originalPath))
            File.WriteAllBytes(originalPath, bytes);

        var embeddedName = originalName;
        var variant = ImageDisplayVariant.TryCreate(bytes);
        if (variant != null)
        {
            var displayName = ImageDisplayVariant.GetDisplayFileName(originalName);
            var displayPath = Path.Combine(outputDirectory, displayName);
            if (!File.Exists(displayPath))
                File.WriteAllBytes(displayPath, variant);
            embeddedName = displayName;
        }

        return new ManifestEntry(key, $"/{relativeDirectory}/{embeddedName}");
    }

    private async Task<byte[]?> DownloadImageAsync(string url, List<string> allowedHosts, CancellationToken ct)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !IsAllowedImageUri(uri, allowedHosts))
        {
            logger.LogWarning("Bild-URL nicht erlaubt (Host-Allowlist/https): {Url}", url);
            return null;
        }

        var client = httpClientFactory.CreateClient(HttpClientName);
        var maxBytes = options.Value.MaxAttachmentBytes;
        var currentUri = uri;

        for (var redirectCount = 0; redirectCount <= MaxRedirects; redirectCount++)
        {
            using var response = await client.GetAsync(currentUri, HttpCompletionOption.ResponseHeadersRead, ct);
            if (IsRedirect(response.StatusCode))
            {
                var location = response.Headers.Location;
                if (location == null)
                    return null;
                currentUri = location.IsAbsoluteUri ? location : new Uri(currentUri, location);
                // Jeder Redirect-Hop wird erneut gegen die Allowlist geprueft (SSRF-Schutz)
                if (!IsAllowedImageUri(currentUri, allowedHosts))
                {
                    logger.LogWarning("Bild-Redirect auf nicht erlaubten Host: {Url}", currentUri);
                    return null;
                }
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Bild-Download fehlgeschlagen ({StatusCode}): {Url}", response.StatusCode, currentUri);
                return null;
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType != null && !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Bild-URL liefert Content-Type {MediaType}: {Url}", mediaType, currentUri);
                return null;
            }

            if (response.Content.Headers.ContentLength is > 0 && response.Content.Headers.ContentLength > maxBytes)
                return null;

            await using var source = await response.Content.ReadAsStreamAsync(ct);
            await using var target = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while ((read = await source.ReadAsync(buffer, ct)) > 0)
            {
                if (target.Length + read > maxBytes)
                {
                    logger.LogWarning("Bild ueberschreitet das Groessenlimit: {Url}", currentUri);
                    return null;
                }
                await target.WriteAsync(buffer.AsMemory(0, read), ct);
            }

            return target.ToArray();
        }

        logger.LogWarning("Zu viele Redirects beim Bild-Download: {Url}", uri);
        return null;
    }

    private byte[]? DecodeBase64(string base64)
    {
        // Groessen-Vorabschaetzung: dekodiert sind es ~3/4 der Base64-Laenge
        if ((long)base64.Length * 3 / 4 > options.Value.MaxAttachmentBytes)
            return null;

        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private void CleanupOrphans(string outputDirectory, HashSet<string> keptKeys)
    {
        foreach (var file in Directory.EnumerateFiles(outputDirectory))
        {
            var name = Path.GetFileName(file);
            if (name.StartsWith("manifest", StringComparison.OrdinalIgnoreCase))
                continue;

            var stem = ImageDisplayVariant.GetFileStem(name);
            if (keptKeys.Contains(stem))
                continue;

            try
            {
                File.Delete(file);
            }
            catch (IOException ex)
            {
                logger.LogWarning(ex, "Verwaistes Bild {File} konnte nicht geloescht werden", file);
            }
        }
    }

    private static async Task<ImageManifest?> ReadManifestAsync(string manifestPath, CancellationToken ct)
    {
        if (!File.Exists(manifestPath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(manifestPath, ct);
            return JsonSerializer.Deserialize<ImageManifest>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task WriteManifestAtomicallyAsync(
        string manifestPath, List<ManifestEntry> entries, CancellationToken ct)
    {
        var temporaryPath = $"{manifestPath}.{Guid.NewGuid():N}.tmp";
        var json = JsonSerializer.Serialize(new ImageManifest(entries), JsonOptions);
        await File.WriteAllTextAsync(temporaryPath, json, ct);
        File.Move(temporaryPath, manifestPath, overwrite: true);
    }

    /// <summary>
    /// Suffix-Match wie ImageProxy:AllowedHosts: ".justimmo.at" matcht Host und alle
    /// Subdomains, Eintraege ohne fuehrenden Punkt nur exakt. Nur https, nur Default-Port.
    /// </summary>
    private static bool IsAllowedImageUri(Uri uri, List<string> allowedHosts)
    {
        if (uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort)
            return false;

        foreach (var allowed in allowedHosts)
        {
            var entry = allowed.Trim();
            if (entry.Length == 0)
                continue;

            if (entry.StartsWith('.'))
            {
                if (uri.Host.EndsWith(entry, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(uri.Host, entry[1..], StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (string.Equals(uri.Host, entry, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Extension aus Magic Bytes - gleiche Formate wie PropertyImageService (jpeg/png/webp).</summary>
    internal static string? DetectImageExtension(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff)
            return ".jpg";
        if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4e && bytes[3] == 0x47)
            return ".png";
        if (bytes.Length >= 12
            && bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F'
            && bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
            return ".webp";
        return null;
    }

    /// <summary>
    /// Dateisystem-sicherer SourceId-Ordnername. OpenImmo-OBIDs koennen beliebige
    /// Zeichen enthalten - gefilterte Ids bekommen einen Hash-Suffix gegen Kollisionen.
    /// </summary>
    internal static string BuildSafeSourceId(string sourceId)
    {
        var filtered = string.Concat(sourceId.Where(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'));
        if (filtered.Length > 40)
            filtered = filtered[..40];

        if (filtered.Length == 0 || !string.Equals(filtered, sourceId, StringComparison.Ordinal))
        {
            var hash = ComputeHash(Encoding.UTF8.GetBytes(sourceId))[..8];
            filtered = filtered.Length == 0 ? hash : $"{filtered}-{hash}";
        }

        return filtered.ToLowerInvariant();
    }

    private static string SafeFeedKey(string feedKey)
        => string.Concat(feedKey.Where(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')).ToLowerInvariant();

    private string GetWebRoot()
        => string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;

    private static bool IsRedirect(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.MovedPermanently or
        HttpStatusCode.Found or
        HttpStatusCode.SeeOther or
        HttpStatusCode.TemporaryRedirect or
        HttpStatusCode.PermanentRedirect;

    private static string ComputeHash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
