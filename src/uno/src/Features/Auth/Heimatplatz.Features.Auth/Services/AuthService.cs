using Heimatplatz.Features.Auth.Contracts.Interfaces;

namespace Heimatplatz.Features.Auth.Services;

/// <summary>
/// Service fuer die Authentifizierung und Token-Verwaltung (In-Memory)
/// </summary>
public class AuthService : IAuthService
{
    private string? _accessToken;
    private string? _refreshToken;
    private Guid? _userId;
    private string? _userEmail;
    private string? _userFullName;
    private DateTimeOffset? _expiresAt;

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

        AuthenticationStateChanged?.Invoke(this, false);
    }
}
