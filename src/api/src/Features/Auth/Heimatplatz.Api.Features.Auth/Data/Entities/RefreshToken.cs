using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.Auth.Data.Entities;

/// <summary>
/// Refresh Token Entity fuer Token-Rotation und -Invalidierung
/// </summary>
public class RefreshToken : BaseEntity
{
    /// <summary>ID des zugehoerigen Benutzers</summary>
    public required Guid UserId { get; set; }

    /// <summary>Der Refresh Token String</summary>
    public required string Token { get; set; }

    /// <summary>Ablaufzeitpunkt des Tokens</summary>
    public required DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Ob der Token widerrufen wurde</summary>
    public bool IsRevoked { get; set; }

    /// <summary>Zeitpunkt der Widerrufung</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>ID des Ersatz-Tokens (bei Token-Rotation)</summary>
    public Guid? ReplacedByTokenId { get; set; }

    /// <summary>Navigation Property zum Benutzer</summary>
    public User? User { get; set; }

    /// <summary>Prueft ob der Token noch gueltig ist</summary>
    public bool IsActive => !IsRevoked && ExpiresAt > DateTimeOffset.UtcNow;
}
