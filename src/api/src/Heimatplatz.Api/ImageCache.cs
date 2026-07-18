using System.Security.Cryptography;
using System.Text;

namespace Heimatplatz.Api;

/// <summary>
/// Best-effort Disk-Cache fuer ausgelieferte Bild-Varianten: Proxy-Downloads externer
/// Quellen und on-demand skalierte lokale Uploads (Listen-Thumbnails). Ohne Cache
/// zahlt jede Erstauslieferung Origin-Roundtrip plus SkiaSharp-Decode/Resize pro Bild.
/// Eintraege sind ueber den Key selbstbeschreibend (Quelle+Breite, bei lokalen Dateien
/// inkl. mtime/Laenge) - Invalidierung passiert ueber Key-Wechsel, alte Eintraege
/// raeumt das Pruning weg. Fehler beim Schreiben werden verschluckt (Cache ist optional).
/// </summary>
public sealed class ImageCache
{
    private static readonly TimeSpan PruneInterval = TimeSpan.FromHours(6);

    // Obergrenze der Staleness fuer externe Proxy-Bilder (deren Key traegt keine
    // Freshness-Komponente): nach spaetestens 14 Tagen wird neu vom Origin geholt.
    private static readonly TimeSpan MaxEntryAge = TimeSpan.FromDays(14);

    private static readonly Dictionary<string, string> ExtensionToContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif"
    };

    private readonly string _cacheDirectory;
    private readonly ILogger<ImageCache> _logger;
    private long _lastPruneTicks;

    public ImageCache(IHostEnvironment environment, ILogger<ImageCache> logger)
    {
        _cacheDirectory = Path.Combine(environment.ContentRootPath, "cache", "images");
        Directory.CreateDirectory(_cacheDirectory);
        _logger = logger;
    }

    /// <summary>Stabiler Cache-Key (SHA-256 hex) aus einer Identitaets-Zeichenkette.</summary>
    public static string ComputeKey(string identity)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));

    /// <summary>Datei-Endung fuer einen Content-Type, null wenn nicht cachebar.</summary>
    public static string? GetExtensionForContentType(string contentType)
    {
        foreach (var (extension, type) in ExtensionToContentType)
        {
            if (string.Equals(type, contentType, StringComparison.OrdinalIgnoreCase))
                return extension;
        }

        return null;
    }

    /// <summary>
    /// Liefert Pfad und Content-Type eines Cache-Treffers. Die Endung traegt den
    /// Content-Type, daher werden alle bekannten Endungen durchprobiert.
    /// </summary>
    public bool TryGet(string key, out string filePath, out string contentType)
    {
        foreach (var (extension, type) in ExtensionToContentType)
        {
            var candidate = Path.Combine(_cacheDirectory, key + extension);
            if (File.Exists(candidate))
            {
                filePath = candidate;
                contentType = type;
                return true;
            }
        }

        filePath = string.Empty;
        contentType = string.Empty;
        return false;
    }

    /// <summary>
    /// Legt einen Eintrag ab (atomar via Temp-Datei + Move). Schreibfehler sind
    /// nicht fatal - der Request wird trotzdem aus dem Speicher bedient.
    /// </summary>
    public async Task StoreAsync(string key, string extension, byte[] bytes, CancellationToken ct = default)
    {
        try
        {
            var targetPath = Path.Combine(_cacheDirectory, key + extension);
            var tempPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";
            await File.WriteAllBytesAsync(tempPath, bytes, ct);
            File.Move(tempPath, targetPath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Bild-Cache-Eintrag konnte nicht geschrieben werden ({Key})", key);
        }

        PruneIfDue();
    }

    /// <summary>
    /// Entfernt abgelaufene Eintraege (und verwaiste Temp-Dateien), hoechstens alle
    /// <see cref="PruneInterval"/> und im Hintergrund - Requests warten nie darauf.
    /// </summary>
    private void PruneIfDue()
    {
        var now = DateTime.UtcNow.Ticks;
        var last = Interlocked.Read(ref _lastPruneTicks);
        if (now - last < PruneInterval.Ticks)
            return;

        if (Interlocked.CompareExchange(ref _lastPruneTicks, now, last) != last)
            return;

        _ = Task.Run(() =>
        {
            try
            {
                var cutoff = DateTime.UtcNow - MaxEntryAge;
                // Temp-Dateien nicht sofort wegraeumen - sie koennten gerade geschrieben werden
                var tempCutoff = DateTime.UtcNow - TimeSpan.FromHours(1);
                var removed = 0;
                foreach (var file in Directory.EnumerateFiles(_cacheDirectory))
                {
                    var fileCutoff = file.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
                        ? tempCutoff
                        : cutoff;
                    if (File.GetLastWriteTimeUtc(file) < fileCutoff)
                    {
                        File.Delete(file);
                        removed++;
                    }
                }

                if (removed > 0)
                    _logger.LogInformation("Bild-Cache-Pruning: {Count} Eintraege entfernt", removed);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Bild-Cache-Pruning fehlgeschlagen");
            }
        });
    }
}
