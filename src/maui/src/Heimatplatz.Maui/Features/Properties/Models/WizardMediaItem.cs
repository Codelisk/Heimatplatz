namespace Heimatplatz.Maui.Features.Properties.Models;

/// <summary>
/// Ein Medien-Element (Foto oder Video) im Inserat-Wizard.
/// Frisch gepickte Elemente tragen die Bytes fuer den Upload; nach dem Upload
/// (bzw. nach dem Fortsetzen eines Entwurfs) referenzieren sie nur noch die
/// Server-URL - so laesst sich der Zustand vollstaendig im Entwurf persistieren.
/// </summary>
public class WizardMediaItem
{
    public string FileName { get; }
    public string ContentType { get; }
    public byte[]? Data { get; }
    public bool IsVideo { get; }

    /// <summary>Server-URL nach erfolgreichem Upload (null = noch nicht hochgeladen)</summary>
    public string? RemoteUrl { get; set; }

    public bool IsPhoto => !IsVideo;
    public bool IsUploaded => RemoteUrl != null;

    private WizardMediaItem(string fileName, string contentType, byte[]? data, bool isVideo, string? remoteUrl)
    {
        FileName = fileName;
        ContentType = contentType;
        Data = data;
        IsVideo = isVideo;
        RemoteUrl = remoteUrl;
    }

    /// <summary>Frisch aus dem MediaPicker uebernommen (Bytes vorhanden, noch kein Upload)</summary>
    public static WizardMediaItem FromPicked(string fileName, string contentType, byte[] data, bool isVideo)
        => new(fileName, contentType, data, isVideo, remoteUrl: null);

    /// <summary>Aus einem gespeicherten Entwurf rekonstruiert (nur URL, keine Bytes)</summary>
    public static WizardMediaItem FromRemote(string url, bool isVideo)
        => new(Path.GetFileName(new Uri(url).LocalPath), string.Empty, data: null, isVideo, remoteUrl: url);

    /// <summary>
    /// Bindbare Vorschau: lokale Bytes fuer neue Fotos, Server-URL nach Resume;
    /// Videos zeigen eine Platzhalter-Kachel.
    /// </summary>
    public ImageSource? DisplaySource => IsVideo
        ? null
        : Data != null
            ? ImageSource.FromStream(() => new MemoryStream(Data))
            : RemoteUrl != null
                ? ImageSource.FromUri(new Uri(RemoteUrl))
                : null;

    /// <summary>Dateigroesse fuer die Anzeige, z.B. "12,4 MB" (leer nach Resume)</summary>
    public string DisplaySize
    {
        get
        {
            if (Data == null)
                return string.Empty;

            var mb = Data.Length / 1024d / 1024d;
            return mb >= 1 ? $"{mb:0.#} MB" : $"{Data.Length / 1024d:0} KB";
        }
    }

    public string ToBase64() => Convert.ToBase64String(
        Data ?? throw new InvalidOperationException("Bereits hochgeladene Medien haben keine lokalen Daten."));
}
