using System.Text;
using System.Text.Json;
using Heimatplatz.Features.Auth.Contracts.Interfaces;

namespace Heimatplatz.Features.Auth.Services;

/// <summary>
/// Service fuer die Authentifizierung und Token-Verwaltung (In-Memory)
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class AuthService : IAuthService
{
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

        AuthenticationStateChanged?.Invoke(this, true);
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

        AuthenticationStateChanged?.Invoke(this, false);
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
