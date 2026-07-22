namespace Heimatplatz.Api.Features.Legal.Contracts.Models;

/// <summary>
/// Vollstaendiges Impressum mit allen Pflichtangaben nach ECG §5 und UGB §14
/// </summary>
public record ImprintDto(
    string CompanyName,
    string LegalForm,
    string Owner,
    string Street,
    string PostalCode,
    string City,
    string Country,
    string Email,
    string? Phone,
    // Phone normalisiert fuer href="tel:..." - berechnet, nicht gespeichert. Ohne das baut
    // das Frontend den Link aus dem Anzeigestring und erzeugt "tel:+43 664 99999999".
    string? PhoneLink,
    string? Website,
    string UidNumber,
    string TaxNumber,
    string? DunsNumber,
    string? Gln,
    string? GisaNumber,
    string Trade,
    string TradeAuthority,
    string ProfessionalLaw,
    string? ChamberMembership,
    string? TradeGroup,
    List<LegalSectionDto> Sections,
    string Version,
    DateTimeOffset EffectiveDate,
    DateTimeOffset LastUpdated
);
