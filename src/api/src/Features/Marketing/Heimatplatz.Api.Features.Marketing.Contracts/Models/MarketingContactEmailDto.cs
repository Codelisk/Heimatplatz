namespace Heimatplatz.Api.Features.Marketing.Contracts.Models;

/// <summary>
/// Zusaetzliche E-Mail-Adresse eines Kontakts neben der Versand-Adresse.
/// <see cref="Source"/>: "Manuell" (im Intern-Bereich erfasst) oder "Posteingang"
/// (vom Sync automatisch gelernt, weil die Absender-Domain eindeutig zum Kontakt gehoert).
/// </summary>
public record MarketingContactEmailDto(
    Guid Id,
    string Email,
    string? Source,
    DateTimeOffset CreatedAt
);
