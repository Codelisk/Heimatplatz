namespace Heimatplatz.Api.Features.Properties.Services;

/// <summary>
/// Loest Anschriften zu WGS84-Koordinaten auf (Grundlage der Kartenansicht im Web).
/// Implementierungen muessen fehlertolerant sein und null statt einer Exception
/// liefern - Anlegen, Update und ZV-Sync duerfen nie am Geocoding scheitern.
/// </summary>
public interface IPropertyGeocoder
{
    /// <summary>
    /// Geocodiert eine Anschrift. Erst wird die Hausanschrift (Strasse) versucht,
    /// dann PLZ/Ort als Ortszentrums-Fallback. Null wenn nichts aufloesbar ist
    /// oder Geocoding deaktiviert ist (Geocoding:Enabled).
    /// </summary>
    Task<PropertyGeocodeResult?> GeocodeAsync(
        string? street,
        string? postalCode,
        string city,
        CancellationToken cancellationToken = default);
}

/// <summary>Ergebnis einer Geocoding-Anfrage.</summary>
/// <param name="IsExact">
/// True = Hausanschrift punktgenau aufgeloest, False = nur Ortszentrum
/// (PLZ/Ort-Fallback). Ungenaue Lagen streut die Karten-API zusaetzlich.
/// </param>
public sealed record PropertyGeocodeResult(double Latitude, double Longitude, bool IsExact);
