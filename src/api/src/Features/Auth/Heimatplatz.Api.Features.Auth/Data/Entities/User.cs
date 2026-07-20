using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.Auth.Data.Entities;

/// <summary>
/// Benutzer-Entity.
/// Jeder Benutzer ist implizit Kaeufer; Verkaeufer ist, wer einen SellerType gesetzt hat.
/// </summary>
public class User : BaseEntity
{
    /// <summary>Vorname des Benutzers</summary>
    public required string FirstName { get; set; }

    /// <summary>Nachname des Benutzers</summary>
    public required string LastName { get; set; }

    /// <summary>E-Mail-Adresse (eindeutig, normalisiert: getrimmt + lowercase)</summary>
    public required string Email { get; set; }

    /// <summary>Gehashtes Passwort</summary>
    public required string PasswordHash { get; set; }

    /// <summary>Vollstaendiger Name</summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Anbietertyp - null = kein Verkaeufer.
    /// Private/Broker/PropertyManager = darf inserieren (Seller-Claim im JWT).
    /// </summary>
    public SellerType? SellerType { get; set; }

    /// <summary>Firmenname (Pflicht bei Broker und PropertyManager)</summary>
    public string? CompanyName { get; set; }

    /// <summary>
    /// Telefonnummer (optional). Fliesst als Erreichbarkeit in den Anbieter-Kontakt
    /// neuer/aktualisierter Inserate ein und wird dort oeffentlich angezeigt.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>Administrator (System-Faehigkeiten wie Batch-Import)</summary>
    public bool IsAdmin { get; set; }

    /// <summary>
    /// Zeitpunkt der E-Mail-Bestaetigung (null = noch nicht bestaetigt).
    /// Bestaetigt wird per Link aus der Verifikations-Mail oder implizit durch einen
    /// erfolgreichen Passwort-Reset (beides beweist Postfach-Besitz).
    /// </summary>
    public DateTimeOffset? EmailVerifiedAt { get; set; }
}
