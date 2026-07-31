using System.IO.Compression;
using Heimatplatz.Api;
using Shiny;

namespace Heimatplatz.Api.Features.OpenImmoImport.Services;

/// <summary>Gefundene Feed-Datei im Drop-Ordner eines Feeds.</summary>
public record OpenImmoFeedFile(
    string FilePath,
    string FileName,
    long FileSize,
    DateTimeOffset LastWriteTimeUtc,
    bool IsStable);

/// <summary>
/// Geoeffneter Feed-Inhalt: XML-Stream plus optionaler ZIP-Zugriff fuer
/// Bild-Entries (null bei reinen XML-Feeds). Dispose schliesst beides.
/// </summary>
public sealed class OpenImmoFeedContent(Stream xmlStream, IOpenImmoZipAccessor? zip, IDisposable? owner) : IDisposable
{
    public Stream XmlStream { get; } = xmlStream;

    public IOpenImmoZipAccessor? Zip { get; } = zip;

    public void Dispose()
    {
        XmlStream.Dispose();
        owner?.Dispose();
    }
}

public interface IOpenImmoZipAccessor
{
    /// <summary>
    /// Liest einen Bild-Entry (Match: exakter Pfad, sonst Dateiname case-insensitiv).
    /// Null wenn nicht gefunden oder groesser als maxBytes.
    /// </summary>
    byte[]? ReadEntry(string entryName, long maxBytes);
}

public interface IOpenImmoFeedReader
{
    /// <summary>
    /// Neueste Feed-Datei (*.xml/*.zip) im Verzeichnis; ignoriert Dotfiles und
    /// FTP-Temporaerdateien. Null wenn keine vorhanden. IsStable=false solange der
    /// letzte Schreibzeitpunkt juenger als stableAge ist (Upload evtl. noch aktiv -
    /// mtime wird waehrend eines FTP-Uploads laufend aktualisiert, ein altes mtime
    /// beweist also einen abgeschlossenen Upload).
    /// </summary>
    OpenImmoFeedFile? FindLatestFeedFile(string feedDirectory, TimeSpan stableAge);

    /// <summary>
    /// Oeffnet eine Feed-Datei (XML direkt oder ZIP mit groesstem XML-Entry).
    /// Wirft InvalidDataException bei kaputtem ZIP, fehlendem XML-Entry oder
    /// Ueberschreiten des Zip-Bomb-Guards.
    /// </summary>
    OpenImmoFeedContent OpenFeedFile(string filePath, long maxArchiveUncompressedBytes);
}

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class OpenImmoFeedReader : IOpenImmoFeedReader
{
    public OpenImmoFeedFile? FindLatestFeedFile(string feedDirectory, TimeSpan stableAge)
    {
        if (!Directory.Exists(feedDirectory))
            return null;

        var candidate = Directory.EnumerateFiles(feedDirectory)
            .Select(path => new FileInfo(path))
            .Where(f => !f.Name.StartsWith('.'))
            .Where(f => f.Extension.ToLowerInvariant() is ".xml" or ".zip")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .FirstOrDefault();

        if (candidate == null)
            return null;

        var isStable = DateTime.UtcNow - candidate.LastWriteTimeUtc >= stableAge;

        return new OpenImmoFeedFile(
            candidate.FullName,
            candidate.Name,
            candidate.Length,
            new DateTimeOffset(candidate.LastWriteTimeUtc, TimeSpan.Zero),
            isStable);
    }

    public OpenImmoFeedContent OpenFeedFile(string filePath, long maxArchiveUncompressedBytes)
    {
        if (!Path.GetExtension(filePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return new OpenImmoFeedContent(
                File.OpenRead(filePath), zip: null, owner: null);
        }

        var archive = ZipFile.OpenRead(filePath);
        try
        {
            // Zip-Bomb-Guard: deklarierte entpackte Gesamtgroesse pruefen, bevor
            // irgendein Entry gelesen wird
            long totalUncompressed = 0;
            foreach (var entry in archive.Entries)
                totalUncompressed += entry.Length;

            if (totalUncompressed > maxArchiveUncompressedBytes)
                throw new InvalidDataException(
                    $"Feed-ZIP entpackt {totalUncompressed} Bytes und ueberschreitet das Limit von {maxArchiveUncompressedBytes} Bytes");

            // Groesster XML-Entry = Objektdaten (manche Producer legen zusaetzliche
            // Meta-XMLs bei)
            var xmlEntry = archive.Entries
                .Where(e => e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.Length)
                .FirstOrDefault()
                ?? throw new InvalidDataException("Feed-ZIP enthaelt keinen XML-Entry");

            // XML komplett in den Speicher: der Stream eines ZipArchiveEntry ist nicht
            // seekbar und das Archiv muss fuer die Bild-Entries offen bleiben
            using var entryStream = xmlEntry.Open();
            var xmlBuffer = new MemoryStream();
            entryStream.CopyTo(xmlBuffer);
            xmlBuffer.Position = 0;

            return new OpenImmoFeedContent(xmlBuffer, new ZipAccessor(archive), archive);
        }
        catch
        {
            archive.Dispose();
            throw;
        }
    }

    private sealed class ZipAccessor(ZipArchive archive) : IOpenImmoZipAccessor
    {
        public byte[]? ReadEntry(string entryName, long maxBytes)
        {
            var entry = archive.GetEntry(entryName)
                ?? archive.Entries.FirstOrDefault(e =>
                    string.Equals(e.Name, Path.GetFileName(entryName), StringComparison.OrdinalIgnoreCase));

            if (entry == null || entry.Length > maxBytes)
                return null;

            using var stream = entry.Open();
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return buffer.ToArray();
        }
    }
}
