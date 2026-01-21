namespace Heimatplatz.Api.Features.Legal.Contracts.Models;

/// <summary>
/// Daten des Verantwortlichen fuer die Datenschutzerklaerung
/// </summary>
public record ResponsiblePartyDto(
    string CompanyName,
    string Street,
    string PostalCode,
    string City,
    string Country,
    string Email,
    string? Phone = null,
    string? DataProtectionOfficer = null
);
