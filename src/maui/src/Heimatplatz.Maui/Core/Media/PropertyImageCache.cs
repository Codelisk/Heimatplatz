using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using Shiny;

namespace Heimatplatz.Maui.Core.Media;

/// <summary>
/// Persistenter Cache fuer bereits angezeigte Immobilienbilder. Anders als der
/// native HTTP-Bildcache liegt er im AppDataDirectory und bleibt damit fuer echte
/// Offline-Detailansichten verfuegbar.
/// </summary>
[Singleton]
public sealed class PropertyImageCache
{
    private const long MaxImageBytes = 20 * 1024 * 1024;
    private const long MaxCacheBytes = 256 * 1024 * 1024;
    private const long TrimmedCacheBytes = 220 * 1024 * 1024;

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    private readonly ConcurrentDictionary<string, Task<string>> _downloads =
        new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _downloadSlots = new(6, 6);
    private readonly SemaphoreSlim _trimGate = new(1, 1);
    private readonly string _cacheDirectory =
        Path.Combine(FileSystem.Current.AppDataDirectory, "property-images-v1");

    public string GetCachedOrOriginal(string url)
    {
        var path = GetCachePath(url);
        return path != null && File.Exists(path) && new FileInfo(path).Length > 0
            ? path
            : url;
    }

    public Task<string> GetOrDownloadAsync(string url, CancellationToken cancellationToken = default)
    {
        var path = GetCachePath(url);
        if (path == null)
            return Task.FromResult(url);

        if (File.Exists(path) && new FileInfo(path).Length > 0)
            return Task.FromResult(path);

        return AwaitSharedDownloadAsync(url, path, cancellationToken);
    }

    private async Task<string> AwaitSharedDownloadAsync(
        string url,
        string path,
        CancellationToken cancellationToken)
    {
        var download = _downloads.GetOrAdd(url, _ => DownloadCoreAsync(url, path));

        try
        {
            return await download.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return url;
        }
        finally
        {
            if (download.IsCompleted)
                _downloads.TryRemove(new KeyValuePair<string, Task<string>>(url, download));
        }
    }

    private async Task<string> DownloadCoreAsync(string url, string path)
    {
        await _downloadSlots.WaitAsync();
        string? temporaryPath = null;

        try
        {
            if (File.Exists(path) && new FileInfo(path).Length > 0)
                return path;

            using var response = await HttpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                CancellationToken.None);
            if (response.StatusCode == HttpStatusCode.NotFound ||
                !response.IsSuccessStatusCode ||
                response.Content.Headers.ContentLength is > MaxImageBytes)
            {
                return url;
            }

            Directory.CreateDirectory(_cacheDirectory);
            temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";

            await using var source = await response.Content.ReadAsStreamAsync();
            await using var target = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            var buffer = new byte[81920];
            long totalBytes = 0;
            while (true)
            {
                var read = await source.ReadAsync(buffer);
                if (read == 0)
                    break;

                totalBytes += read;
                if (totalBytes > MaxImageBytes)
                    return url;

                await target.WriteAsync(buffer.AsMemory(0, read));
            }

            await target.FlushAsync();
            target.Close();

            if (totalBytes == 0)
                return url;

            File.Move(temporaryPath, path, overwrite: true);
            temporaryPath = null;
            _ = TrimCacheAsync(path);
            return path;
        }
        catch (Exception) when (File.Exists(path))
        {
            return path;
        }
        catch
        {
            // Das Bild bleibt ueber seine Remote-URL nutzbar. Ein Bildfehler darf
            // weder Karten noch Detailseiten in einen Fehlerzustand versetzen.
            return url;
        }
        finally
        {
            if (temporaryPath != null)
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch
                {
                    // Temp-Cleanup ist Best Effort.
                }
            }

            _downloadSlots.Release();
        }
    }

    private string? GetCachePath(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(url)));
        var extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant() switch
        {
            ".jpeg" => ".jpeg",
            ".png" => ".png",
            ".webp" => ".webp",
            _ => ".jpg"
        };
        return Path.Combine(_cacheDirectory, $"{hash}{extension}");
    }

    private async Task TrimCacheAsync(string currentPath)
    {
        await _trimGate.WaitAsync();
        try
        {
            if (!Directory.Exists(_cacheDirectory))
                return;

            var files = new DirectoryInfo(_cacheDirectory)
                .EnumerateFiles()
                .Where(file => !file.Name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ToList();
            var totalBytes = files.Sum(file => file.Length);
            if (totalBytes <= MaxCacheBytes)
                return;

            foreach (var file in files.AsEnumerable().Reverse())
            {
                if (totalBytes <= TrimmedCacheBytes ||
                    string.Equals(file.FullName, currentPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    totalBytes -= file.Length;
                    file.Delete();
                }
                catch
                {
                    // Geoeffnete oder bereits entfernte Dateien beim naechsten Lauf.
                }
            }
        }
        catch
        {
            // Cachepflege darf den erfolgreichen Bildabruf nie beeinflussen.
        }
        finally
        {
            _trimGate.Release();
        }
    }
}
