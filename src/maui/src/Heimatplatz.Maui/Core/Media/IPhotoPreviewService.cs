namespace Heimatplatz.Maui.Core.Media;

/// <summary>
/// Erzeugt kleine, EXIF-korrekt orientierte Vorschaubilder fuer die Anzeige
/// frisch gepickter Fotos. Das Original bleibt unangetastet (voller Qualitaet
/// fuer den Upload); ohne Vorschau wuerden 100+-MP-Kamerafotos beim Rendern
/// Androids Canvas-Limit sprengen (Crash "trying to draw too large bitmap").
/// </summary>
public interface IPhotoPreviewService
{
    /// <summary>
    /// Dekodiert das Foto unter <paramref name="filePath"/> speicherschonend
    /// auf maximal <paramref name="maxDimension"/> Pixel Kantenlaenge und
    /// liefert JPEG-Bytes. Faellt bei nicht dekodierbaren Formaten auf die
    /// Originaldatei zurueck.
    /// </summary>
    Task<byte[]> CreatePreviewAsync(string filePath, int maxDimension = 1600, CancellationToken ct = default);
}
