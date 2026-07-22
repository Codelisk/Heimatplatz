namespace Heimatplatz.Api.Features.Legal.Contracts.Models;

/// <summary>
/// Aufbereitete Kontaktdaten fuer alle Frontends (Web-Footer, Makler-Seite, JSON-LD, MAUI).
///
/// Zusammengesetzt aus dem Impressum (Pflichtangaben nach ECG §5 - Firma, Adresse, E-Mail,
/// Telefon, Website) und den optionalen Zusatzfeldern des "Contact"-Datensatzes
/// (<see cref="ContactSettingsDto"/>). Frontends muessen nichts mehr selbst ableiten:
/// SupportEmail ist bereits aufgeloest und PhoneLink fertig normalisiert (Backend-First).
///
/// Leere/nicht gepflegte Angaben kommen als null bzw. leere Liste - die Frontends blenden
/// die jeweilige Zeile dann aus, statt Platzhalter anzuzeigen.
/// </summary>
public record LegalContactInfoDto(
    string CompanyName,
    string Street,
    string PostalCode,
    string City,
    string Country,
    // Allgemeine bzw. Impressum-Adresse
    string Email,
    // Adresse fuer Nutzeranfragen - faellt auf Email zurueck, wenn nichts Eigenes
    // gepflegt ist, also nie leer
    string SupportEmail,
    // Telefonnummer in Anzeigeform, z.B. "+43 664 73221804"
    string? Phone,
    // Dieselbe Nummer normalisiert fuer href="tel:...", z.B. "+4366473221804"
    string? PhoneLink,
    string? Website,
    // Freitext zur Erreichbarkeit, z.B. "Mo-Fr 9-17 Uhr"
    string? OfficeHours,
    List<SocialLinkDto> SocialLinks
);
