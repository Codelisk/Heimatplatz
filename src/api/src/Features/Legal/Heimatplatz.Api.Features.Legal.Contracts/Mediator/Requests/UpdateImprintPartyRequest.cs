using Heimatplatz.Api.Features.Legal.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Legal.Contracts.Mediator.Requests;

/// <summary>
/// Aendert die Firmendaten im Impressum (ECG §5 / UGB §14) - Admin-only.
///
/// Vollstaendiges Ersetzen der Party-Felder; die Rechtstext-Abschnitte (Haftungsausschluss,
/// Urheberrecht, Streitschlichtung) bleiben unangetastet. Das Intern-Formular laedt den
/// aktuellen Stand und schickt ihn komplett zurueck - ohne Prefill wuerden Felder geleert.
/// </summary>
public record UpdateImprintPartyRequest(
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
) : IRequest<UpdateImprintPartyResponse>;

public record UpdateImprintPartyResponse(
    bool Success,
    string? Error,
    ImprintDto? Imprint
);
