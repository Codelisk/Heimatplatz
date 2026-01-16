using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Handlers;

/// <summary>
/// Handler fuer LoginRequest - authentifiziert Benutzer und gibt Tokens zurueck
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class LoginHandler(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService
) : IRequestHandler<LoginRequest, LoginResponse>
{
    [MediatorHttpPost("/api/auth/login", OperationId = "Login")]
    public async Task<LoginResponse> Handle(LoginRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        // Benutzer per Email suchen inkl. Rollen
        var user = await dbContext.Set<User>()
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Ungueltige E-Mail-Adresse oder Passwort.");
        }

        // Passwort verifizieren
        if (!passwordHasher.Verify(request.Passwort, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Ungueltige E-Mail-Adresse oder Passwort.");
        }

        // Rollen des Benutzers ermitteln
        var roles = user.Roles.Select(r => r.RoleType);

        // Tokens generieren mit Rollen
        var accessToken = tokenService.GenerateAccessToken(user, roles);
        var refreshTokenString = tokenService.GenerateRefreshToken();
        var refreshValidityHours = tokenService.GetRefreshTokenValidityHours();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(refreshValidityHours);

        // Refresh Token in DB speichern
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Set<RefreshToken>().Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResponse(
            accessToken,
            refreshTokenString,
            user.Id,
            user.Email,
            user.FullName,
            expiresAt
        );
    }
}
