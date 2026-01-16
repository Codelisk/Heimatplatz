using Heimatplatz.Api.Features.Auth.Data.Entities;

namespace Heimatplatz.Api.Features.Auth.Services;

/// <summary>
/// Service fuer JWT Token-Generierung und -Validierung
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generiert einen Access Token fuer den Benutzer
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
