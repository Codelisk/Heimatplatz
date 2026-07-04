namespace Heimatplatz.Maui.Features.Properties.Models;

/// <summary>
/// Ein Medien-Element (Foto oder Video) fuer die KI-gestuetzte Inseratserstellung.
/// </summary>
public record AiMediaItem(
    string FileName,
    string ContentType,
    byte[] Data,
    bool IsVideo
)
{
    public bool IsPhoto => !IsVideo;

    /// <summary>
    /// Bindbare Vorschau: Bilddaten fuer Fotos; Videos zeigen eine Platzhalter-Kachel.
    /// </summary>
    public ImageSource? DisplaySource => IsVideo
        ? null
        : ImageSource.FromStream(() => new MemoryStream(Data));

    /// <summary>Dateigroesse fuer die Anzeige, z.B. "12,4 MB"</summary>
    public string DisplaySize
    {
        get
        {
            var mb = Data.Length / 1024d / 1024d;
            return mb >= 1 ? $"{mb:0.#} MB" : $"{Data.Length / 1024d:0} KB";
        }
    }

    public string ToBase64() => Convert.ToBase64String(Data);
}
