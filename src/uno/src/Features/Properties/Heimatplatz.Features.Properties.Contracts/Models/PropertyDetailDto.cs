namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>
/// DTO fuer Immobilien-Detailansicht mit erweiterten Informationen
/// </summary>
public record PropertyDetailDto(
    Guid Id,
    string Titel,
    string Adresse,
    string Ort,
    decimal Preis,
    int? WohnflaecheM2,
    int? GrundstuecksflaecheM2,
    int? Zimmer,
    int? Baujahr,
    PropertyType Typ,
    SellerType AnbieterTyp,
    string AnbieterName,
    string? AnbieterTelefon,
    string? AnbieterEmail,
    List<string> BildUrls,
    string? Beschreibung,
    List<string> Ausstattung
);
