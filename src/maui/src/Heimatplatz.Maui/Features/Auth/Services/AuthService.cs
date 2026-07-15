using System.Text;
using System.Text.Json;
using Shiny;

namespace Heimatplatz.Maui.Features.Auth.Services;

/// <summary>
/// Service fuer die Authentifizierung und Token-Verwaltung.
/// Persistenz via Shiny Stores (<see cref="AuthSession"/>), Tokens im Secure Store.
/// </summary>
[Singleton]
public class AuthService(AuthSession session) : IAuthService
{
    private DateTimeOffset? _expiresAt;
    private bool _expiresAtLoaded;
    private bool _isSeller;
    private bool _isBuyer;
    private bool _rolesLoaded;

    /// <inheritdoc />
    // Ein abgelaufener Access-Token beendet die lokale Session nicht. Online erneuert
    // TokenRefreshMiddleware ihn vor dem Request; offline darf der Benutzer weiterhin
    // auf seine lokal gespeicherten Daten zugreifen. Erst ein abgelehnter Refresh oder
    // ein expliziter Logout loescht die Session.
    public bool IsAuthenticated =>
        !string.IsNullOrEmpty(session.AccessToken) &&
        !string.IsNullOrEmpty(session.RefreshToken) &&
        Guid.TryParse(session.UserId, out _) &&
        ExpiresAtValue is not null;

    /// <inheritdoc />
    public string? AccessToken => session.AccessToken;

    /// <inheritdoc />
    public string? RefreshToken => session.RefreshToken;

    /// <inheritdoc />
    public Guid? UserId => Guid.TryParse(session.UserId, out var id) ? id : null;

    /// <inheritdoc />
    public string? UserEmail => session.UserEmail;

    /// <inheritdoc />
    public string? UserFullName => session.UserFullName;

    /// <inheritdoc />
    public bool IsSeller
    {
        get
        {
            EnsureRolesLoaded();
            return _isSeller;
        }
    }

    /// <inheritdoc />
    public bool IsBuyer
    {
        get
        {
            EnsureRolesLoaded();
            return _isBuyer;
        }
    }

    /// <inheritdoc />
    public event EventHandler<bool>? AuthenticationStateChanged;

    private DateTimeOffset? ExpiresAtValue
    {
        get
        {
            if (!_expiresAtLoaded)
            {
                _expiresAt = DateTimeOffset.TryParse(session.ExpiresAt, out var parsed) ? parsed : null;
                _expiresAtLoaded = true;
            }
            return _expiresAt;
        }
    }

    /// <inheritdoc />
    public void SetAuthenticatedUser(
        string accessToken,
        string refreshToken,
        Guid userId,
        string email,
        string fullName,
        DateTimeOffset expiresAt)
    {
        session.AccessToken = accessToken;
        session.RefreshToken = refreshToken;
        session.UserId = userId.ToString();
        session.UserEmail = email;
        session.UserFullName = fullName;
        session.ExpiresAt = expiresAt.ToString("O");
        _expiresAt = expiresAt;
        _expiresAtLoaded = true;

        ExtractRolesFromToken(accessToken);

        AuthenticationStateChanged?.Invoke(this, true);
    }

    /// <inheritdoc />
    public void UpdateTokens(string accessToken, string refreshToken, DateTimeOffset expiresAt)
    {
        session.AccessToken = accessToken;
        session.RefreshToken = refreshToken;
        session.ExpiresAt = expiresAt.ToString("O");
        _expiresAt = expiresAt;
        _expiresAtLoaded = true;

        ExtractRolesFromToken(accessToken);
    }

    /// <inheritdoc />
    public void ClearAuthentication()
    {
        session.Clear();
        _expiresAt = null;
        _expiresAtLoaded = true;
        _isSeller = false;
        _isBuyer = false;
        _rolesLoaded = true;

        AuthenticationStateChanged?.Invoke(this, false);
    }

    /// <inheritdoc />
    public Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            // Access-Token darf abgelaufen sein - wird via TokenRefreshMiddleware erneuert.
            // Ohne Refresh-Token ist keine Wiederherstellung moeglich.
            if (string.IsNullOrEmpty(session.AccessToken) ||
                string.IsNullOrEmpty(session.RefreshToken) ||
                string.IsNullOrEmpty(session.UserId) ||
                string.IsNullOrEmpty(session.ExpiresAt))
            {
                session.Clear();
                return Task.FromResult(false);
            }

            if (!Guid.TryParse(session.UserId, out _) ||
                !DateTimeOffset.TryParse(session.ExpiresAt, out _))
            {
                session.Clear();
                return Task.FromResult(false);
            }

            ExtractRolesFromToken(session.AccessToken);
            AuthenticationStateChanged?.Invoke(this, true);
            return Task.FromResult(true);
        }
        catch
        {
            session.Clear();
            return Task.FromResult(false);
        }
    }

    private void EnsureRolesLoaded()
    {
        if (_rolesLoaded)
            return;

        if (!string.IsNullOrEmpty(session.AccessToken))
            ExtractRolesFromToken(session.AccessToken);
        else
            _rolesLoaded = true;
    }

    /// <summary>
    /// Extrahiert die Rollen aus dem JWT-Token Payload (Base64-kodiertes JSON)
    /// </summary>
    private void ExtractRolesFromToken(string token)
    {
        _isSeller = false;
        _isBuyer = false;
        _rolesLoaded = true;

        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2)
                return;

            // JWT Payload ist Base64Url-kodiert
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
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
                    SetRoleFlags(roleElement.GetString());
                }
                else if (roleElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in roleElement.EnumerateArray())
                        SetRoleFlags(item.GetString());
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
