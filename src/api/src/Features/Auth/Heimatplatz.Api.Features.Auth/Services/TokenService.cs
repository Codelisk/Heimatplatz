using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Shiny;

namespace Heimatplatz.Api.Features.Auth.Services;

/// <summary>
/// Claim-Types fuer Benutzerinformationen
/// </summary>
public static class HeimatplatzClaimTypes
{
    /// <summary>Claim-Type fuer Benutzerrollen (Seller, Admin). Kaeufer ist implizit = authentifiziert.</summary>
    public const string UserRole = "user_role";

    /// <summary>Claim-Type fuer den Anbietertyp (Private, Broker, PropertyManager)</summary>
    public const string SellerType = "seller_type";
}

/// <summary>
/// JWT Token Service - generiert Access und Refresh Tokens
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly SymmetricSecurityKey _signingKey;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;

        var key = _configuration["Authentication:Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key nicht konfiguriert");

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }

    public string GenerateAccessToken(User user)
    {
        var issuer = _configuration["Authentication:Jwt:Issuer"];
        var audience = _configuration["Authentication:Jwt:Audience"];
        var validityMinutes = _configuration.GetValue<int>("Authentication:Jwt:TokenValidityMinutes", 10);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Verkaeufer: user_role=Seller + konkreter Anbietertyp
        if (user.SellerType != null)
        {
            claims.Add(new Claim(HeimatplatzClaimTypes.UserRole, "Seller"));
            claims.Add(new Claim(HeimatplatzClaimTypes.SellerType, user.SellerType.Value.ToString()));
        }

        if (user.IsAdmin)
        {
            claims.Add(new Claim(HeimatplatzClaimTypes.UserRole, "Admin"));
        }

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(validityMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    public string HashRefreshToken(string refreshToken)
    {
        // SHA-256 ohne Salt reicht hier: der Token ist bereits ein 64-Byte-Zufallswert
        // (kein Woerterbuch-/Brute-Force-Risiko). Gespeichert wird nur der Hash -
        // ein DB-Leak erlaubt damit keine Session-Uebernahme.
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(hash);
    }

    public int GetRefreshTokenValidityHours()
    {
        return _configuration.GetValue<int>("Authentication:Jwt:RefreshValidityHours", 720);
    }
}
