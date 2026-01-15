namespace Heimatplatz.Api.Features.Properties.Contracts;

/// <summary>
/// Vollstaendige Immobilien-Daten fuer Detailansicht
/// </summary>
public record PropertyDto(
    Guid Id,
    string Titel,
    string Adresse,
    string Ort,
    string Plz,
    decimal Preis,
    int? WohnflaecheM2,
    int? GrundstuecksflaecheM2,
    int? Zimmer,
    int? Baujahr,
    PropertyType Typ,
    SellerType AnbieterTyp,
    string AnbieterName,
    string? Beschreibung,
    List<string> Ausstattung,
    List<string> BildUrls,
    DateTimeOffset ErstelltAm
);

/// <summary>
/// Kompakte Immobilien-Daten fuer Listenansicht
/// </summary>
public record PropertyListItemDto(
    Guid Id,
    string Titel,
    string Adresse,
    string Ort,
    decimal Preis,
    int? WohnflaecheM2,
    int? GrundstuecksflaecheM2,
    int? Zimmer,
    PropertyType Typ,
    SellerType AnbieterTyp,
    string AnbieterName,
    List<string> BildUrls
);
