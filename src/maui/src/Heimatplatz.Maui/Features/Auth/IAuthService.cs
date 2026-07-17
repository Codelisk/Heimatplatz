namespace Heimatplatz.Maui.Features.Auth;

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
    /// Der aktuelle Refresh Token (null wenn nicht angemeldet)
    /// </summary>
    string? RefreshToken { get; }

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
    /// Gibt an, ob der Benutzer Verkäufer ist (SellerType im Profil gesetzt).
    /// Käufer ist jeder angemeldete Benutzer implizit.
    /// </summary>
    bool IsSeller { get; }

    /// <summary>
    /// Der Anbietertyp aus dem JWT-Claim seller_type ("Private", "Broker", "PropertyManager"),
    /// null wenn kein Verkäufer
    /// </summary>
    string? SellerType { get; }

    /// <summary>
    /// Gibt an, ob der Benutzer Administrator ist
    /// </summary>
    bool IsAdmin { get; }

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
    /// Aktualisiert nur die Tokens (nach erfolgreichem Refresh).
    /// Aendert nicht userId/email/fullName und loest kein AuthenticationStateChanged aus.
    /// </summary>
    void UpdateTokens(string accessToken, string refreshToken, DateTimeOffset expiresAt);

    /// <summary>
    /// Loescht alle Authentifizierungsdaten (Logout)
    /// </summary>
    void ClearAuthentication();

    /// <summary>
    /// Event das ausgeloest wird wenn sich der Authentifizierungsstatus aendert
    /// </summary>
    event EventHandler<bool>? AuthenticationStateChanged;

    /// <summary>
    /// Aktualisiert nur den Access Token (z.B. nach Profil-Update, das neue Claims liefert).
    /// Refresh Token und Ablaufdatum bleiben unveraendert.
    /// </summary>
    void UpdateAccessToken(string accessToken);

    /// <summary>
    /// Versucht eine gespeicherte Session wiederherzustellen (beim App-Start aufrufen)
    /// </summary>
    /// <returns>True wenn eine gueltige Session wiederhergestellt wurde</returns>
    Task<bool> TryRestoreSessionAsync();
}
