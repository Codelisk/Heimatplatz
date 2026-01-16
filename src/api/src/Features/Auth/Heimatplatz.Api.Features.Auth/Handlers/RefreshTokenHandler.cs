using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Handlers;

/// <summary>
/// Handler fuer RefreshTokenRequest - erneuert Access Token mittels Refresh Token
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class RefreshTokenHandler(
    AppDbContext dbContext,
    ITokenService tokenService
) : IRequestHandler<RefreshTokenRequest, RefreshTokenResponse>
{
    [MediatorHttpPost("/api/auth/refresh", OperationId = "RefreshToken")]
    public async Task<RefreshTokenResponse> Handle(RefreshTokenRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        // Refresh Token in DB suchen inkl. User und dessen Rollen
        var storedToken = await dbContext.Set<RefreshToken>()
            .Include(rt => rt.User)
                .ThenInclude(u => u!.Roles)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (storedToken == null)
        {
            throw new UnauthorizedAccessException("Ungueltiger Refresh Token.");
        }

        // Pruefen ob Token noch aktiv ist
        if (!storedToken.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh Token ist abgelaufen oder wurde widerrufen.");
        }

        if (storedToken.User == null)
        {
            throw new UnauthorizedAccessException("Benutzer nicht gefunden.");
        }

        // Alten Token widerrufen (Token Rotation)
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTimeOffset.UtcNow;

        // Rollen des Benutzers ermitteln
        var roles = storedToken.User.Roles.Select(r => r.RoleType);

        // Neue Tokens generieren mit Rollen
        var accessToken = tokenService.GenerateAccessToken(storedToken.User, roles);
        var newRefreshTokenString = tokenService.GenerateRefreshToken();
        var refreshValidityHours = tokenService.GetRefreshTokenValidityHours();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(refreshValidityHours);

        // Neuen Refresh Token erstellen
        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = storedToken.UserId,
            Token = newRefreshTokenString,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Referenz zum Ersatz-Token setzen
        storedToken.ReplacedByTokenId = newRefreshToken.Id;

        dbContext.Set<RefreshToken>().Add(newRefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponse(
            accessToken,
            newRefreshTokenString,
            expiresAt
        );
    }
}
