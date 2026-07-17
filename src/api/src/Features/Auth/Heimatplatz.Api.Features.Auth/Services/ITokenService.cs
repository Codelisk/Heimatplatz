using Heimatplatz.Api.Features.Auth.Data.Entities;

namespace Heimatplatz.Api.Features.Auth.Services;

/// <summary>
/// Service fuer JWT Token-Generierung und -Validierung
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generiert einen Access Token fuer den Benutzer.
    /// Claims werden direkt aus dem User abgeleitet:
    /// user_role=Seller wenn SellerType gesetzt, user_role=Admin wenn IsAdmin,
    /// seller_type mit dem konkreten Anbietertyp.
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generiert einen kryptografisch sicheren Refresh Token
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Hasht einen Refresh Token fuer die DB-Ablage (SHA-256, hex).
    /// In der Datenbank liegt nur der Hash - der Klartext geht ausschliesslich an den Client.
    /// </summary>
    string HashRefreshToken(string refreshToken);

    /// <summary>
    /// Gibt die konfigurierte Refresh Token Gueltigkeit in Stunden zurueck
    /// </summary>
    int GetRefreshTokenValidityHours();
}
