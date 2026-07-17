using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.Auth.Data.Entities;

/// <summary>
/// Zweck eines UserActionTokens
/// </summary>
public enum UserTokenPurpose
{
    /// <summary>Bestaetigung der E-Mail-Adresse (Link in der Verifikations-Mail)</summary>
    EmailVerification = 1,

    /// <summary>Passwort zuruecksetzen ("Passwort vergessen"-Mail)</summary>
    PasswordReset = 2
}

/// <summary>
/// Einmal-Token fuer E-Mail-gestuetzte Aktionen (Verifikation, Passwort-Reset).
/// Gespeichert wird NUR der SHA-256-Hash - der Klartext-Token steht ausschliesslich
/// im Link der versendeten Mail. Ein DB-Leak verraet damit keine gueltigen Links.
/// </summary>
public class UserActionToken : BaseEntity
{
    /// <summary>ID des zugehoerigen Benutzers</summary>
    public required Guid UserId { get; set; }

    /// <summary>SHA-256-Hash des Klartext-Tokens (Hex, 64 Zeichen)</summary>
    public required string TokenHash { get; set; }

    /// <summary>Wofuer der Token gilt</summary>
    public required UserTokenPurpose Purpose { get; set; }

    /// <summary>Ablaufzeitpunkt</summary>
    public required DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Zeitpunkt der Einloesung (null = noch nicht eingeloest, Tokens sind Einmal-Tokens)</summary>
    public DateTimeOffset? UsedAt { get; set; }

    /// <summary>Navigation Property zum Benutzer</summary>
    public User? User { get; set; }
}
