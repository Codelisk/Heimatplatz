namespace Heimatplatz.Maui.Features.Properties.Models;

/// <summary>
/// Repraesentiert ein Bild fuer die Anzeige in der UI.
/// Neue Bilder referenzieren das unveraenderte Original als Cache-Datei (voller
/// Qualitaet fuer den Upload) und tragen eine kleine EXIF-korrigierte Vorschau
/// fuer die Anzeige; bestehende Bilder haben eine Url.
/// </summary>
public record ImageItem(
    string FileName,
    string ContentType,
    string? LocalPath = null,
    byte[]? PreviewData = null,
    string? Url = null
)
{
    /// <summary>
    /// True wenn das Bild bereits am Server liegt (hat URL, keine lokale Datei).
    /// </summary>
    public bool IsExisting => Url != null;

    /// <summary>
    /// Bindbare Bildquelle: kleine Vorschau fuer neue lokale Bilder, URL fuer
    /// bestehende Server-Bilder. Das Original wird nie direkt gerendert
    /// (100+-MP-Fotos sprengen Androids Canvas-Limit).
    /// </summary>
    public ImageSource? DisplaySource => IsExisting
        ? ImageSource.FromUri(new Uri(Url!))
        : PreviewData != null
            ? ImageSource.FromStream(() => new MemoryStream(PreviewData))
            : null;

    /// <summary>
    /// Liest das unveraenderte Original fuer den Upload von der Platte.
    /// </summary>
    public async Task<string> ToBase64Async(CancellationToken ct = default)
    {
        var bytes = await File.ReadAllBytesAsync(
            LocalPath ?? throw new InvalidOperationException("Bestehende Server-Bilder haben keine lokalen Daten."),
            ct);
        return Convert.ToBase64String(bytes);
    }
}
