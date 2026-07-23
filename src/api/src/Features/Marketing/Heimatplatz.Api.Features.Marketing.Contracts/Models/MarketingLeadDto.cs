namespace Heimatplatz.Api.Features.Marketing.Contracts.Models;

/// <summary>
/// Eine Firma aus dem Firmenbuch-Katalog als moeglicher Kontakt. Das Firmenbuch fuehrt
/// keine Kontaktdaten - E-Mail/Telefon entstehen erst im CRM.
/// <see cref="ContactId"/> ist gesetzt, sobald die Firma uebernommen wurde.
/// </summary>
public record MarketingLeadDto(
    Guid Id,
    string Fnr,
    string Name,
    string? Sitz,
    string? RechtsformText,
    Guid? ContactId,
    MarketingContactStatus? ContactStatus
);
