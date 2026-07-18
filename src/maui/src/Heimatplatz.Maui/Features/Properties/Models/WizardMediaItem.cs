namespace Heimatplatz.Maui.Features.Properties.Models;

/// <summary>
/// Ein Medien-Element (Foto oder Video) im Inserat-Wizard.
/// Frisch gepickte Fotos referenzieren das unveraenderte Original als Datei im
/// App-Cache (voller Qualitaet fuer den Upload) und tragen eine kleine,
/// EXIF-korrigierte Vorschau fuer die Anzeige - das Original selbst darf nie
/// gerendert werden (100+-MP-Fotos sprengen Androids Canvas-Limit).
/// Nach dem Upload (bzw. nach dem Fortsetzen eines Entwurfs) referenzieren sie
/// nur noch die Server-URL - so laesst sich der Zustand im Entwurf persistieren.
/// </summary>
public class WizardMediaItem
{
    public string FileName { get; }
    public string ContentType { get; }
    public bool IsVideo { get; }

    /// <summary>Pfad zum unveraenderten Original im App-Cache (null nach Resume)</summary>
    public string? LocalPath { get; }

    /// <summary>Kleine Vorschau nur fuer die Anzeige (null nach Resume)</summary>
    public byte[]? PreviewData { get; }

    private readonly long _localFileSize;

    /// <summary>Server-URL nach erfolgreichem Upload (null = noch nicht hochgeladen)</summary>
    public string? RemoteUrl { get; set; }

    public bool IsPhoto => !IsVideo;
    public bool IsUploaded => RemoteUrl != null;
    public bool HasLocalFile => LocalPath != null && File.Exists(LocalPath);

    private WizardMediaItem(
        string fileName,
        string contentType,
        string? localPath,
        byte[]? previewData,
        bool isVideo,
        string? remoteUrl)
    {
        FileName = fileName;
        ContentType = contentType;
        LocalPath = localPath;
        PreviewData = previewData;
        IsVideo = isVideo;
        RemoteUrl = remoteUrl;
        _localFileSize = localPath != null && File.Exists(localPath)
            ? new FileInfo(localPath).Length
            : 0;
    }

    /// <summary>Frisch aus dem MediaPicker uebernommen (Original im Cache, noch kein Upload)</summary>
    public static WizardMediaItem FromPicked(string fileName, string contentType, string localPath, byte[]? previewData, bool isVideo)
        => new(fileName, contentType, localPath, previewData, isVideo, remoteUrl: null);

    /// <summary>Aus einem gespeicherten Entwurf rekonstruiert (nur URL, keine lokale Datei)</summary>
    public static WizardMediaItem FromRemote(string url, bool isVideo)
        => new(Path.GetFileName(new Uri(url).LocalPath), string.Empty, localPath: null, previewData: null, isVideo, remoteUrl: url);

    /// <summary>
    /// Bindbare Vorschau: lokale Vorschau-Bytes fuer neue Fotos, Server-URL nach
    /// Resume; Videos zeigen eine Platzhalter-Kachel.
    /// </summary>
    public ImageSource? DisplaySource => IsVideo
        ? null
        : PreviewData != null
            ? ImageSource.FromStream(() => new MemoryStream(PreviewData))
            : RemoteUrl != null
                ? ImageSource.FromUri(new Uri(RemoteUrl))
                : null;

    /// <summary>Dateigroesse fuer die Anzeige, z.B. "12,4 MB" (leer nach Resume)</summary>
    public string DisplaySize
    {
        get
        {
            if (_localFileSize == 0)
                return string.Empty;

            var mb = _localFileSize / 1024d / 1024d;
            return mb >= 1 ? $"{mb:0.#} MB" : $"{_localFileSize / 1024d:0} KB";
        }
    }

    /// <summary>Liest das unveraenderte Original fuer den Upload von der Platte.</summary>
    public Task<byte[]> ReadOriginalAsync(CancellationToken ct = default)
        => File.ReadAllBytesAsync(
            LocalPath ?? throw new InvalidOperationException("Bereits hochgeladene Medien haben keine lokalen Daten."),
            ct);

    /// <summary>Entfernt die Cache-Kopie des Originals (nach Upload oder Loeschen).</summary>
    public void TryDeleteLocalFile()
    {
        try
        {
            if (LocalPath != null && File.Exists(LocalPath))
                File.Delete(LocalPath);
        }
        catch (IOException)
        {
            // Cache-Datei raeumt sonst das OS auf
        }
    }
}
