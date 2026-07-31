using System.Security.Cryptography;
using System.Text.Json;
using System.Xml;
using Heimatplatz.Api.Features.OpenImmoImport.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.OpenImmoImport.Services;

/// <summary>
/// Prozessweiter Lauf-Guard, geteilt von Worker und Trigger-Handler
/// (Muster TriggerForeclosureAuctionSyncHandler._syncRunning).
/// </summary>
public static class OpenImmoImportGuard
{
    private static int _running;

    public static bool IsRunning => Volatile.Read(ref _running) == 1;

    public static bool TryEnter() => Interlocked.CompareExchange(ref _running, 1, 0) == 0;

    public static void Exit() => Interlocked.Exchange(ref _running, 0);
}

/// <summary>
/// Marker-File je Feed ({StateRoot}/{feedKey}/last-import.json): Identitaet der zuletzt
/// erfolgreich importierten Feed-Datei plus Ergebnis-Zusammenfassung. Liegt bewusst
/// AUSSERHALB des FTP-Chroots (State-Verzeichnis), damit der Absender ihn nicht
/// manipulieren kann.
/// </summary>
public record OpenImmoImportMarker(
    string FileName,
    long FileSize,
    DateTimeOffset LastWriteTimeUtc,
    string ContentSha256,
    DateTimeOffset ImportedAtUtc,
    string Summary);

public static class OpenImmoMarkerStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string GetMarkerPath(string stateRootPath, string feedKey)
        => Path.Combine(stateRootPath, feedKey, "last-import.json");

    public static async Task<OpenImmoImportMarker?> ReadAsync(
        string stateRootPath, string feedKey, CancellationToken ct = default)
    {
        var path = GetMarkerPath(stateRootPath, feedKey);
        if (!File.Exists(path))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(path, ct);
            return JsonSerializer.Deserialize<OpenImmoImportMarker>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static async Task WriteAsync(
        string stateRootPath, string feedKey, OpenImmoImportMarker marker, CancellationToken ct = default)
    {
        var path = GetMarkerPath(stateRootPath, feedKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(marker, JsonOptions), ct);
        File.Move(temporaryPath, path, overwrite: true);
    }
}

/// <summary>Ergebnis eines Feed-Laufs fuer Log und Trigger-Antwort.</summary>
public record OpenImmoFeedRunResult(
    string FeedKey,
    OpenImmoFeedRunOutcome Outcome,
    string? FileName = null,
    OpenImmoSyncResult? Sync = null,
    string? Error = null);

public enum OpenImmoFeedRunOutcome
{
    /// <summary>Import durchgelaufen (Marker aktualisiert).</summary>
    Imported = 1,

    /// <summary>Keine Feed-Datei im Drop-Ordner.</summary>
    NoFile = 2,

    /// <summary>Datei zu frisch (Upload evtl. noch aktiv) - naechster Tick versucht es erneut.</summary>
    NotStable = 3,

    /// <summary>Datei seit dem letzten Lauf unveraendert (Marker-Kurzschluss).</summary>
    Unchanged = 4,

    /// <summary>Lauf fehlgeschlagen - Marker NICHT aktualisiert, naechster Tick versucht es erneut.</summary>
    Failed = 5
}

public interface IOpenImmoImportService
{
    /// <summary>True wenn IncomingRootPath und mindestens ein Feed konfiguriert sind.</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Laeuft ueber alle konfigurierten Feeds. Null wenn bereits ein Lauf aktiv ist
    /// (prozessweiter Guard, geteilt von Worker und Trigger).
    /// </summary>
    Task<List<OpenImmoFeedRunResult>?> TryRunAllFeedsAsync(bool force = false, CancellationToken ct = default);
}

/// <summary>
/// Orchestrator eines Import-Laufs: Datei finden → Stabilitaet/Marker pruefen →
/// Arbeitskopie ziehen (Justimmo darf die Drop-Datei jederzeit ueberschreiben) →
/// parsen → syncen → Marker schreiben. Fehler eines Feeds beeintraechtigen andere
/// Feeds nicht.
/// </summary>
public class OpenImmoImportService(
    IOpenImmoFeedReader feedReader,
    IOpenImmoParser parser,
    IOpenImmoPropertySyncService syncService,
    IOptions<OpenImmoImportOptions> options,
    ILogger<OpenImmoImportService> logger
) : IOpenImmoImportService
{
    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(options.Value.IncomingRootPath)
        && options.Value.Feeds.Count > 0;

    public async Task<List<OpenImmoFeedRunResult>?> TryRunAllFeedsAsync(
        bool force = false, CancellationToken ct = default)
    {
        if (!IsEnabled)
            return [];

        if (!OpenImmoImportGuard.TryEnter())
        {
            logger.LogInformation("[OpenImmoImport] Lauf uebersprungen - Import laeuft bereits");
            return null;
        }

        try
        {
            var results = new List<OpenImmoFeedRunResult>();
            foreach (var feed in options.Value.Feeds)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    results.Add(await RunFeedAsync(feed, force, ct));
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[OpenImmoImport] Feed {FeedKey} fehlgeschlagen", feed.Key);
                    results.Add(new OpenImmoFeedRunResult(feed.Key, OpenImmoFeedRunOutcome.Failed, Error: ex.Message));
                }
            }

            return results;
        }
        finally
        {
            OpenImmoImportGuard.Exit();
        }
    }

    private async Task<OpenImmoFeedRunResult> RunFeedAsync(
        OpenImmoFeedOptions feed, bool force, CancellationToken ct)
    {
        var feedDirectory = Path.Combine(options.Value.IncomingRootPath, feed.Key);
        var stateRoot = options.Value.ResolveStateRootPath();

        var feedFile = feedReader.FindLatestFeedFile(
            feedDirectory, TimeSpan.FromSeconds(options.Value.FileStableSeconds));

        if (feedFile == null)
            return new OpenImmoFeedRunResult(feed.Key, OpenImmoFeedRunOutcome.NoFile);

        if (!feedFile.IsStable)
        {
            logger.LogInformation(
                "[OpenImmoImport] Feed {FeedKey}: {FileName} ist juenger als {StableSeconds}s - warte auf Upload-Abschluss",
                feed.Key, feedFile.FileName, options.Value.FileStableSeconds);
            return new OpenImmoFeedRunResult(feed.Key, OpenImmoFeedRunOutcome.NotStable, feedFile.FileName);
        }

        var marker = await OpenImmoMarkerStore.ReadAsync(stateRoot, feed.Key, ct);
        if (!force
            && marker != null
            && marker.FileName == feedFile.FileName
            && marker.FileSize == feedFile.FileSize
            && marker.LastWriteTimeUtc == feedFile.LastWriteTimeUtc)
        {
            return new OpenImmoFeedRunResult(feed.Key, OpenImmoFeedRunOutcome.Unchanged, feedFile.FileName);
        }

        // Arbeitskopie: die Drop-Datei kann von Justimmo jederzeit ueberschrieben
        // werden - geparst wird immer die Kopie. Das geaenderte mtime der Drop-Datei
        // triggert dann den naechsten Lauf.
        var workDirectory = Path.Combine(stateRoot, feed.Key, "work");
        Directory.CreateDirectory(workDirectory);
        var workCopyPath = Path.Combine(workDirectory, $"current{Path.GetExtension(feedFile.FileName)}");
        File.Copy(feedFile.FilePath, workCopyPath, overwrite: true);

        var contentHash = await ComputeFileHashAsync(workCopyPath, ct);
        if (!force && marker != null && marker.ContentSha256 == contentHash)
        {
            // Inhalt identisch (z.B. erneuter Push derselben Daten): nur die
            // Datei-Identitaet im Marker nachziehen, kein Import
            await OpenImmoMarkerStore.WriteAsync(stateRoot, feed.Key, marker with
            {
                FileName = feedFile.FileName,
                FileSize = feedFile.FileSize,
                LastWriteTimeUtc = feedFile.LastWriteTimeUtc
            }, ct);
            return new OpenImmoFeedRunResult(feed.Key, OpenImmoFeedRunOutcome.Unchanged, feedFile.FileName);
        }

        OpenImmoSyncResult syncResult;
        try
        {
            using var content = feedReader.OpenFeedFile(workCopyPath, options.Value.MaxArchiveUncompressedBytes);
            var parseResult = parser.Parse(content.XmlStream);

            foreach (var warning in parseResult.Warnings)
                logger.LogInformation("[OpenImmoImport] Feed {FeedKey}: {Warning}", feed.Key, warning);

            syncResult = await syncService.SyncAsync(feed, parseResult, content.Zip, ct);
        }
        catch (Exception ex) when (ex is XmlException or InvalidDataException)
        {
            // Kaputte Datei: Marker NICHT schreiben - vielleicht war der Upload doch
            // unvollstaendig, der naechste Push repariert das von selbst
            logger.LogError(ex, "[OpenImmoImport] Feed {FeedKey}: {FileName} nicht lesbar", feed.Key, feedFile.FileName);
            return new OpenImmoFeedRunResult(
                feed.Key, OpenImmoFeedRunOutcome.Failed, feedFile.FileName, Error: ex.Message);
        }

        if (syncResult.Aborted)
        {
            return new OpenImmoFeedRunResult(
                feed.Key, OpenImmoFeedRunOutcome.Failed, feedFile.FileName, syncResult,
                Error: string.Join("; ", syncResult.ErrorMessages));
        }

        // Einzelfehler (syncResult.Errors > 0) verhindern den Marker bewusst nicht:
        // die Datei wurde verarbeitet, ein Retry derselben Datei aendert nichts
        // (Force-Flag existiert fuer Haendisches)
        await OpenImmoMarkerStore.WriteAsync(stateRoot, feed.Key, new OpenImmoImportMarker(
            feedFile.FileName,
            feedFile.FileSize,
            feedFile.LastWriteTimeUtc,
            contentHash,
            DateTimeOffset.UtcNow,
            syncResult.ToString()), ct);

        logger.LogInformation(
            "[OpenImmoImport] Feed {FeedKey}: {FileName} importiert ({Result})",
            feed.Key, feedFile.FileName, syncResult);

        return new OpenImmoFeedRunResult(feed.Key, OpenImmoFeedRunOutcome.Imported, feedFile.FileName, syncResult);
    }

    private static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
