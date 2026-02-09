namespace Heimatplatz.Features.Auth.Contracts.Interfaces;

/// <summary>
/// Service fuer die Authentifizierung und Token-Verwaltung
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Gibt an, ob der Benutzer angemeldet ist
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Der aktuelle Access Token (null wenn nicht angemeldet)
    /// </summary>
    string? AccessToken { get; }

    /// <summary>
    /// Die aktuelle Benutzer-ID (null wenn nicht angemeldet)
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Die E-Mail des angemeldeten Benutzers
    /// </summary>
    string? UserEmail { get; }

    /// <summary>
    /// Der vollstaendige Name des angemeldeten Benutzers
    /// </summary>
    string? UserFullName { get; }

    /// <summary>
    /// Gibt an, ob der Benutzer die Rolle Verkaeufer hat
    /// </summary>
    bool IsSeller { get; }

    /// <summary>
    /// Gibt an, ob der Benutzer die Rolle Kaeufer hat
    /// </summary>
    bool IsBuyer { get; }

    /// <summary>
    /// Speichert die Login-Daten nach erfolgreicher Authentifizierung
    /// </summary>
    void SetAuthenticatedUser(
        string accessToken,
        string refreshToken,
        Guid userId,
        string email,
        string fullName,
        DateTimeOffset expiresAt);

    /// <summary>
    /// Loescht alle Authentifizierungsdaten (Logout)
    /// </summary>
    void ClearAuthentication();

    /// <summary>
    /// Event das ausgeloest wird wenn sich der Authentifizierungsstatus aendert
    /// </summary>
    event EventHandler<bool>? AuthenticationStateChanged;

    /// <summary>
    /// Versucht eine gespeicherte Session wiederherzustellen (beim App-Start aufrufen)
    /// </summary>
    /// <returns>True wenn eine gueltige Session wiederhergestellt wurde</returns>
    Task<bool> TryRestoreSessionAsync();
}
