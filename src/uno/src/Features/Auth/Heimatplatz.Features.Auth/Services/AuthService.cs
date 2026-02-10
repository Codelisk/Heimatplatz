using System.Text;
using System.Text.Json;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Windows.Storage;

namespace Heimatplatz.Features.Auth.Services;

/// <summary>
/// Service fuer die Authentifizierung und Token-Verwaltung mit persistenter Speicherung
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class AuthService : IAuthService
{
    // Storage Keys fuer LocalSettings
    private const string AccessTokenKey = "auth_access_token";
    private const string RefreshTokenKey = "auth_refresh_token";
    private const string UserIdKey = "auth_user_id";
    private const string UserEmailKey = "auth_user_email";
    private const string UserFullNameKey = "auth_user_fullname";
    private const string ExpiresAtKey = "auth_expires_at";

    private string? _accessToken;
    private string? _refreshToken;
    private Guid? _userId;
    private string? _userEmail;
    private string? _userFullName;
    private DateTimeOffset? _expiresAt;
    private bool _isSeller;
    private bool _isBuyer;

    /// <inheritdoc />
    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && _expiresAt > DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public string? AccessToken => _accessToken;

    /// <inheritdoc />
    public string? RefreshToken => _refreshToken;

    /// <inheritdoc />
    public Guid? UserId => _userId;

    /// <inheritdoc />
    public string? UserEmail => _userEmail;

    /// <inheritdoc />
    public string? UserFullName => _userFullName;

    /// <inheritdoc />
    public bool IsSeller => _isSeller;

    /// <inheritdoc />
    public bool IsBuyer => _isBuyer;

    /// <inheritdoc />
    public event EventHandler<bool>? AuthenticationStateChanged;

    /// <inheritdoc />
    public void SetAuthenticatedUser(
        string accessToken,
        string refreshToken,
        Guid userId,
        string email,
        string fullName,
        DateTimeOffset expiresAt)
    {
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        _userId = userId;
        _userEmail = email;
        _userFullName = fullName;
        _expiresAt = expiresAt;

        // Rollen aus JWT-Token extrahieren
        ExtractRolesFromToken(accessToken);

        // Persistent speichern
        SaveToStorage();

        AuthenticationStateChanged?.Invoke(this, true);
    }

    /// <inheritdoc />
    public void UpdateTokens(string accessToken, string refreshToken, DateTimeOffset expiresAt)
    {
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        _expiresAt = expiresAt;

        ExtractRolesFromToken(accessToken);
        SaveToStorage();
    }

    /// <inheritdoc />
    public void ClearAuthentication()
    {
        _accessToken = null;
        _refreshToken = null;
        _userId = null;
        _userEmail = null;
        _userFullName = null;
        _expiresAt = null;
        _isSeller = false;
        _isBuyer = false;

        // Aus Storage loeschen
        ClearStorage();

        AuthenticationStateChanged?.Invoke(this, false);
    }

    /// <inheritdoc />
    public Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;

            // Pruefen ob gespeicherte Daten vorhanden sind
            if (!settings.Values.ContainsKey(AccessTokenKey))
                return Task.FromResult(false);

            var accessToken = settings.Values[AccessTokenKey] as string;
            var refreshToken = settings.Values[RefreshTokenKey] as string;
            var userIdStr = settings.Values[UserIdKey] as string;
            var email = settings.Values[UserEmailKey] as string;
            var fullName = settings.Values[UserFullNameKey] as string;
            var expiresAtStr = settings.Values[ExpiresAtKey] as string;

            // Validierung der Pflichtfelder
            if (string.IsNullOrEmpty(accessToken) ||
                string.IsNullOrEmpty(userIdStr) ||
                string.IsNullOrEmpty(expiresAtStr))
            {
                ClearStorage();
                return Task.FromResult(false);
            }

            // Parsen
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                ClearStorage();
                return Task.FromResult(false);
            }

            if (!DateTimeOffset.TryParse(expiresAtStr, out var expiresAt))
            {
                ClearStorage();
                return Task.FromResult(false);
            }

            // Pruefen ob Refresh-Token vorhanden ist
            // Access-Token darf abgelaufen sein - wird via TokenRefreshMiddleware erneuert
            if (string.IsNullOrEmpty(refreshToken))
            {
                ClearStorage();
                return Task.FromResult(false);
            }

            // Session wiederherstellen (auch mit abgelaufenem Access-Token)
            _accessToken = accessToken;
            _refreshToken = refreshToken;
            _userId = userId;
            _userEmail = email;
            _userFullName = fullName;
            _expiresAt = expiresAt;

            // Rollen aus Token extrahieren
            ExtractRolesFromToken(accessToken);

            // Event ausloesen
            AuthenticationStateChanged?.Invoke(this, true);

            return Task.FromResult(true);
        }
        catch
        {
            // Bei Fehlern Storage loeschen und false zurueckgeben
            ClearStorage();
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Speichert die Auth-Daten in LocalSettings
    /// </summary>
    private void SaveToStorage()
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;

            settings.Values[AccessTokenKey] = _accessToken;
            settings.Values[RefreshTokenKey] = _refreshToken;
            settings.Values[UserIdKey] = _userId?.ToString();
            settings.Values[UserEmailKey] = _userEmail;
            settings.Values[UserFullNameKey] = _userFullName;
            settings.Values[ExpiresAtKey] = _expiresAt?.ToString("O"); // ISO 8601 Format
        }
        catch
        {
            // Fehler beim Speichern ignorieren - App funktioniert weiter, nur ohne Persistenz
        }
    }

    /// <summary>
    /// Loescht die Auth-Daten aus LocalSettings
    /// </summary>
    private void ClearStorage()
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;

            settings.Values.Remove(AccessTokenKey);
            settings.Values.Remove(RefreshTokenKey);
            settings.Values.Remove(UserIdKey);
            settings.Values.Remove(UserEmailKey);
            settings.Values.Remove(UserFullNameKey);
            settings.Values.Remove(ExpiresAtKey);
        }
        catch
        {
            // Fehler beim Loeschen ignorieren
        }
    }

    /// <summary>
    /// Extrahiert die Rollen aus dem JWT-Token Payload (Base64-kodiertes JSON)
    /// </summary>
    private void ExtractRolesFromToken(string token)
    {
        _isSeller = false;
        _isBuyer = false;

        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2)
                return;

            // JWT Payload ist Base64Url-kodiert
            var payload = parts[1];
            // Base64Url zu Base64 konvertieren
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var jsonBytes = Convert.FromBase64String(payload);
            var json = Encoding.UTF8.GetString(jsonBytes);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // user_role Claim kann als String oder Array vorliegen
            if (root.TryGetProperty("user_role", out var roleElement))
            {
                if (roleElement.ValueKind == JsonValueKind.String)
                {
                    var role = roleElement.GetString();
                    SetRoleFlags(role);
                }
                else if (roleElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in roleElement.EnumerateArray())
                    {
                        SetRoleFlags(item.GetString());
                    }
                }
            }
        }
        catch
        {
            // Fehler beim Parsen ignorieren - Rollen bleiben false
        }
    }

    private void SetRoleFlags(string? role)
    {
        if (string.Equals(role, "Seller", StringComparison.OrdinalIgnoreCase))
            _isSeller = true;
        else if (string.Equals(role, "Buyer", StringComparison.OrdinalIgnoreCase))
            _isBuyer = true;
    }
}
