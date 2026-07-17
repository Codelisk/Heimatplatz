using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Handlers;

/// <summary>
/// Handler fuer RefreshTokenRequest - erneuert Access Token mittels Refresh Token
/// </summary>
[AllowAnonymous]
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class RefreshTokenHandler(
    AppDbContext dbContext,
    ITokenService tokenService
) : IRequestHandler<RefreshTokenRequest, RefreshTokenResponse>
{
    [MediatorHttpPost("/api/auth/refresh", OperationId = "RefreshToken")]
    public async Task<RefreshTokenResponse> Handle(RefreshTokenRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        // Refresh Token in DB suchen inkl. User (gespeichert ist nur der SHA-256-Hash)
        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await dbContext.Set<RefreshToken>()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == tokenHash, cancellationToken);

        if (storedToken == null)
        {
            throw new UnauthorizedAccessException("Ungueltiger Refresh Token.");
        }

        // Pruefen ob Token noch aktiv ist
        if (!storedToken.IsActive)
        {
            // Reuse-Detection: Ein bereits rotierter/widerrufener Token wird erneut
            // praesentiert - klassisches Zeichen fuer einen gestohlenen Token (der
            // legitime Client haelt laengst den Nachfolger). Sicherheitshalber die
            // gesamte Token-Familie des Benutzers widerrufen.
            if (storedToken.IsRevoked)
            {
                var activeTokens = await dbContext.Set<RefreshToken>()
                    .Where(rt => rt.UserId == storedToken.UserId && !rt.IsRevoked)
                    .ToListAsync(cancellationToken);

                var now = DateTimeOffset.UtcNow;
                foreach (var token in activeTokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = now;
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            throw new UnauthorizedAccessException("Refresh Token ist abgelaufen oder wurde widerrufen.");
        }

        if (storedToken.User == null)
        {
            throw new UnauthorizedAccessException("Benutzer nicht gefunden.");
        }

        // Alten Token widerrufen (Token Rotation)
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTimeOffset.UtcNow;

        // Neue Tokens generieren (Claims kommen direkt aus dem User)
        var accessToken = tokenService.GenerateAccessToken(storedToken.User);
        var newRefreshTokenString = tokenService.GenerateRefreshToken();
        var refreshValidityHours = tokenService.GetRefreshTokenValidityHours();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(refreshValidityHours);

        // Neuen Refresh Token erstellen (nur als Hash - Klartext geht nur an den Client)
        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = storedToken.UserId,
            Token = tokenService.HashRefreshToken(newRefreshTokenString),
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Referenz zum Ersatz-Token setzen
        storedToken.ReplacedByTokenId = newRefreshToken.Id;

        dbContext.Set<RefreshToken>().Add(newRefreshToken);

        // Opportunistisches Aufraeumen: Tokens dieses Benutzers, deren Ablaufzeit
        // vorbei ist, loeschen - sonst waechst die Tabelle mit jeder Rotation
        // unbegrenzt. Widerrufene Tokens bleiben bis zu ihrem urspruenglichen
        // Ablauf erhalten, damit die Reuse-Detection oben greifen kann.
        var expiredTokens = await dbContext.Set<RefreshToken>()
            .Where(rt => rt.UserId == storedToken.UserId && rt.ExpiresAt <= DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);
        dbContext.Set<RefreshToken>().RemoveRange(expiredTokens);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponse(
            accessToken,
            newRefreshTokenString,
            expiresAt
        );
    }
}
