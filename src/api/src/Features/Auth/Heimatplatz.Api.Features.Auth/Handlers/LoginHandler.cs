using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Microsoft.EntityFrameworkCore;
using Shiny;
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
        // Benutzer per normalisierter E-Mail suchen (Registrierung speichert lowercase)
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        var user = await dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Ungueltige E-Mail-Adresse oder Passwort.");
        }

        // Passwort verifizieren
        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Ungueltige E-Mail-Adresse oder Passwort.");
        }

        // Tokens generieren (Claims kommen direkt aus dem User: Seller/Admin/SellerType)
        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshTokenString = tokenService.GenerateRefreshToken();
        var refreshValidityHours = tokenService.GetRefreshTokenValidityHours();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(refreshValidityHours);

        // Refresh Token in DB speichern (nur als Hash - Klartext geht nur an den Client)
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = tokenService.HashRefreshToken(refreshTokenString),
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
