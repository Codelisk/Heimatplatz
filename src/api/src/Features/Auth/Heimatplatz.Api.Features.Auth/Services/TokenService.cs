using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Heimatplatz.Api.Features.Auth.Contracts.Enums;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Shiny.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Auth.Services;

/// <summary>
/// Claim-Type fuer Benutzerrollen
/// </summary>
public static class HeimatplatzClaimTypes
{
    /// <summary>Claim-Type fuer Benutzerrollen (Buyer, Seller)</summary>
    public const string UserRole = "user_role";
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

    public string GenerateAccessToken(User user, IEnumerable<UserRoleType>? roles = null)
    {
        var issuer = _configuration["Authentication:Jwt:Issuer"];
        var audience = _configuration["Authentication:Jwt:Audience"];
        var validityMinutes = _configuration.GetValue<int>("Authentication:Jwt:TokenValidityMinutes", 10);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.Vorname),
            new(JwtRegisteredClaimNames.FamilyName, user.Nachname),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Benutzerrollen als Claims hinzufuegen
        if (roles != null)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(HeimatplatzClaimTypes.UserRole, role.ToString()));
            }
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

    public int GetRefreshTokenValidityHours()
    {
        return _configuration.GetValue<int>("Authentication:Jwt:RefreshValidityHours", 720);
    }
}
