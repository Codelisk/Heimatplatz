namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>
/// DTO fuer Immobilien-Listenansicht
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
