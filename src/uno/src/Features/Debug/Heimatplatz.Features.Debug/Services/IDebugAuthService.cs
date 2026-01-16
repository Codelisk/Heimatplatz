namespace Heimatplatz.Features.Debug.Services;

/// <summary>
/// Service fuer Debug-Authentifizierung - Quick Login fuer Test-User
/// </summary>
public interface IDebugAuthService
{
    /// <summary>
    /// Schneller Login mit bekannten Test-Credentials
    /// </summary>
    Task<bool> QuickLoginAsync(string email, string password);

    /// <summary>
    /// Logout des aktuellen Users
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Aktuellen JWT-Token abrufen
    /// </summary>
    string? GetCurrentToken();

    /// <summary>
    /// Aktuelle User-Email abrufen
    /// </summary>
    string? GetCurrentUserEmail();

    /// <summary>
    /// Aktuelle User-Rollen abrufen
    /// </summary>
    string GetCurrentUserRoles();
}
