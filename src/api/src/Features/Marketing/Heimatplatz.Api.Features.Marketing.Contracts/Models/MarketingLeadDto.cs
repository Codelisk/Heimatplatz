namespace Heimatplatz.Api.Features.Marketing.Contracts.Models;

/// <summary>
/// Eine Firma aus dem Firmenpool als moeglicher Kontakt. Die Daten kommen live aus der
/// Firmenpool-API - Heimatplatz speichert Firmen erst, wenn sie als Kontakt uebernommen
/// werden (Schluessel ist die Firmenbuchnummer). Das Firmenbuch fuehrt keine Kontaktdaten -
/// E-Mail/Telefon entstehen erst im CRM.
/// <see cref="ContactId"/> ist gesetzt, sobald die Firma uebernommen wurde.
/// </summary>
public record MarketingLeadDto(
    string Fnr,
    string Name,
    string? Sitz,
    string? RechtsformText,
    Guid? ContactId,
    MarketingContactStatus? ContactStatus
);
