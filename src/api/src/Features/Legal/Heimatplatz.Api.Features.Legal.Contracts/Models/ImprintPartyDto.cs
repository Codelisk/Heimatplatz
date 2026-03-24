namespace Heimatplatz.Api.Features.Legal.Contracts.Models;

/// <summary>
/// Firmendaten fuer das Impressum - wird in ResponsiblePartyJson gespeichert
/// </summary>
public record ImprintPartyDto(
    string CompanyName,
    string LegalForm,
    string Owner,
    string Street,
    string PostalCode,
    string City,
    string Country,
    string Email,
    string? Phone,
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
    string? TradeGroup
);
