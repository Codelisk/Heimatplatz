namespace Heimatplatz.Api.Features.Legal.Contracts.Models;

/// <summary>
/// Die Zusatz-Kontaktdaten, die im LegalSettings-Datensatz mit SettingType "Contact"
/// gespeichert werden (ResponsiblePartyJson).
///
/// Bewusst NUR Ergaenzungen und Overrides: Firma und Adresse kommen immer aus dem
/// Impressum, damit es fuer die Pflichtangaben genau eine Quelle gibt. Jedes Feld hier
/// ist optional - leer bedeutet "Impressum-Wert verwenden" bzw. "nicht anzeigen".
/// </summary>
public record ContactSettingsDto(
    // Override der allgemeinen E-Mail-Adresse - leer = Impressum-Adresse
    string? Email = null,
    // Eigene Adresse fuer Nutzeranfragen - leer = allgemeine Adresse
    string? SupportEmail = null,
    // Override der Telefonnummer - leer = Impressum-Telefon
    string? Phone = null,
    // Override der Website - leer = Impressum-Website
    string? Website = null,
    // Freitext zur Erreichbarkeit, z.B. "Mo-Fr 9-17 Uhr"
    string? OfficeHours = null,
    List<SocialLinkDto>? SocialLinks = null
);
