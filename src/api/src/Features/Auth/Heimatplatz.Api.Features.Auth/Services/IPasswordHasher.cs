namespace Heimatplatz.Api.Features.Auth.Services;

/// <summary>
/// Service fuer Passwort-Hashing
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hasht ein Passwort
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Verifiziert ein Passwort gegen einen Hash
    /// </summary>
    bool Verify(string password, string hash);
}
