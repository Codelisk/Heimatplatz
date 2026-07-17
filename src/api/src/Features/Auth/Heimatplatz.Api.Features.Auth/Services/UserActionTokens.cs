using System.Security.Cryptography;
using System.Text;

namespace Heimatplatz.Api.Features.Auth.Services;

/// <summary>
/// Erzeugung und Hashing der Einmal-Tokens fuer E-Mail-Verifikation und Passwort-Reset.
/// Der Klartext-Token (256 Bit Zufall, hex-codiert und damit URL-safe) geht nur in den
/// Mail-Link; in der DB liegt ausschliesslich der SHA-256-Hash (siehe UserActionToken).
/// </summary>
public static class UserActionTokens
{
    /// <summary>Verifikations-Links sind bewusst grosszuegig gueltig (Mail wird oft spaeter gelesen)</summary>
    public static readonly TimeSpan EmailVerificationValidity = TimeSpan.FromDays(3);

    /// <summary>Reset-Links kurz halten - sie erlauben die Konto-Uebernahme</summary>
    public static readonly TimeSpan PasswordResetValidity = TimeSpan.FromHours(2);

    /// <summary>Erzeugt einen neuen Klartext-Token (64 Hex-Zeichen)</summary>
    public static string GenerateToken() =>
        Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

    /// <summary>SHA-256-Hash eines Klartext-Tokens (64 Hex-Zeichen) fuer Speicherung/Lookup</summary>
    public static string HashToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
