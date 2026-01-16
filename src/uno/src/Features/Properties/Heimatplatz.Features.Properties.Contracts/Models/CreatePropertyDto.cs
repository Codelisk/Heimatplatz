using Heimatplatz.Features.Properties.Contracts.Models;

namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>
/// DTO zum Erstellen einer neuen Immobilie
/// </summary>
public record CreatePropertyDto(
    string Titel,
    string Adresse,
    string Ort,
    string Plz,
    decimal Preis,
    PropertyType Typ,
    SellerType AnbieterTyp,
    string AnbieterName,
    string? Beschreibung = null,
    int? WohnflaecheM2 = null,
    int? GrundstuecksflaecheM2 = null,
    int? Zimmer = null,
    int? Baujahr = null,
    List<string>? Ausstattung = null,
    List<string>? BildUrls = null
);

/// <summary>
/// Response nach erfolgreicher Erstellung
/// </summary>
public record CreatePropertyResponseDto(
    Guid PropertyId,
    string Titel,
    DateTimeOffset CreatedAt
);
