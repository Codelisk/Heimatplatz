using Shiny;

namespace Heimatplatz.Maui.Features.Auth.Services;

/// <summary>
/// Persistente Auth-Session via Shiny Stores.
/// Tokens landen im Secure Store (Keychain/Keystore), Profildaten im Default Store.
/// Properties persistieren automatisch beim Setzen ([Bind] Source Generator).
/// </summary>
[Singleton]
public partial class AuthSession
{
    [Bind("secure")]
    public partial string? AccessToken { get; set; }

    [Bind("secure")]
    public partial string? RefreshToken { get; set; }

    [Bind]
    public partial string? UserId { get; set; }

    [Bind]
    public partial string? UserEmail { get; set; }

    [Bind]
    public partial string? UserFullName { get; set; }

    /// <summary>ISO-8601 ("O") formatiert</summary>
    [Bind]
    public partial string? ExpiresAt { get; set; }

    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
        UserId = null;
        UserEmail = null;
        UserFullName = null;
        ExpiresAt = null;
    }
}
