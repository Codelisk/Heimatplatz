using Heimatplatz.Api.Features.Auth.Contracts.Enums;
using Heimatplatz.Api.Features.Auth.Data.Entities;

namespace Heimatplatz.Api.Features.Auth.Services;

/// <summary>
/// Service fuer JWT Token-Generierung und -Validierung
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generiert einen Access Token fuer den Benutzer mit seinen Rollen
    /// </summary>
    /// <param name="user">Der Benutzer</param>
    /// <param name="roles">Die Rollen des Benutzers (Buyer, Seller)</param>
    string GenerateAccessToken(User user, IEnumerable<UserRoleType>? roles = null);

    /// <summary>
    /// Generiert einen kryptografisch sicheren Refresh Token
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Gibt die konfigurierte Refresh Token Gueltigkeit in Stunden zurueck
    /// </summary>
    int GetRefreshTokenValidityHours();
}
