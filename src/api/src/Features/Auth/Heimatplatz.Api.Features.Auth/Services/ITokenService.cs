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
    /// Gibt die konfigurierte Refresh Token Gueltigkeit in Stunden zurueck
    /// </summary>
    int GetRefreshTokenValidityHours();
}
