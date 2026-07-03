namespace Heimatplatz.Maui.Features.Properties.Models;

/// <summary>
/// Repraesentiert ein Bild fuer die Anzeige in der UI.
/// Neue Bilder haben byte[]-Daten fuer den Upload; bestehende Bilder haben eine Url.
/// </summary>
public record ImageItem(
    string FileName,
    string ContentType,
    byte[] Data,
    string? Url = null
)
{
    /// <summary>
    /// True wenn das Bild bereits am Server liegt (hat URL, keine Daten).
    /// </summary>
    public bool IsExisting => Url != null;

    /// <summary>
    /// Bindbare Bildquelle: Stream fuer neue lokale Bilder, URL fuer bestehende Server-Bilder.
    /// </summary>
    public ImageSource DisplaySource => IsExisting
        ? ImageSource.FromUri(new Uri(Url!))
        : ImageSource.FromStream(() => new MemoryStream(Data));

    /// <summary>
    /// Liefert die Bilddaten als Base64-String fuer den JSON-Upload.
    /// </summary>
    public string ToBase64() => Convert.ToBase64String(Data);
}
