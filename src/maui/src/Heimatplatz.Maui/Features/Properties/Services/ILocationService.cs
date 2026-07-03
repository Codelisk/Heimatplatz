namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Service fuer das Laden der Location-Hierarchie (Bundesland -> Bezirk -> Gemeinde).
/// Cached die Daten nach dem ersten Laden.
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Laedt die Location-Hierarchie (cached nach erstem Aufruf)
    /// </summary>
    Task<List<LocationBundeslandDto>> GetLocationsAsync(CancellationToken ct = default);

    /// <summary>
    /// Ermittelt eine MunicipalityId anhand von Ortsname und/oder PLZ.
    /// Liefert null wenn kein Treffer gefunden wurde.
    /// </summary>
    Task<Guid?> ResolveMunicipalityIdAsync(string? cityName, string? postalCode, CancellationToken ct = default);

    /// <summary>
    /// Liefert alle Gemeinden flach aus der Location-Hierarchie.
    /// </summary>
    Task<List<LocationGemeindeDto>> GetAllMunicipalitiesAsync(CancellationToken ct = default);
}

/// <summary>
/// Bundesland DTO fuer Frontend
/// </summary>
public record LocationBundeslandDto(
    Guid Id,
    string Key,
    string Name,
    List<LocationBezirkDto> Bezirke
);

/// <summary>
/// Bezirk DTO fuer Frontend
/// </summary>
public record LocationBezirkDto(
    Guid Id,
    string Key,
    string Name,
    Guid BundeslandId,
    List<LocationGemeindeDto> Gemeinden
);

/// <summary>
/// Gemeinde DTO fuer Frontend
/// </summary>
public record LocationGemeindeDto(
    Guid Id,
    string Key,
    string Name,
    string PostalCode,
    string? Status,
    Guid BezirkId
);
