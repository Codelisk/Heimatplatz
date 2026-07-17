using System.IdentityModel.Tokens.Jwt;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Exceptions;
using Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Handlers;

/// <summary>
/// Handler zum Aendern des eigenen Passworts (POST /api/auth/change-password).
/// Erfordert das aktuelle Passwort und widerruft alle bestehenden Refresh Tokens
/// (andere Geraete werden abgemeldet), danach wird ein frisches Token-Paar ausgestellt.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class ChangePasswordHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IPasswordHasher passwordHasher,
    ITokenService tokenService
) : IRequestHandler<ChangePasswordRequest, ChangePasswordResponse>
{
    [MediatorHttpPost("/api/auth/change-password", OperationId = "ChangePassword", RequiresAuthorization = true)]
    public async Task<ChangePasswordResponse> Handle(ChangePasswordRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();

        var user = await dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Benutzer nicht gefunden.");

        if (string.IsNullOrEmpty(request.CurrentPassword) ||
            !passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new ValidationException("Das aktuelle Passwort ist nicht korrekt.");
        }

        var newPassword = UserInputValidator.ValidatePassword(request.NewPassword);

        user.PasswordHash = passwordHasher.Hash(newPassword);
        user.UpdatedAt = DateTimeOffset.UtcNow;

        // Alle Refresh Tokens widerrufen - gestohlene/alte Sessions enden hier.
        // (DateTimeOffset-Vergleiche sind auf SQLite nicht uebersetzbar, daher ohne
        // Ablauf-Filter: abgelaufene Tokens zusaetzlich zu widerrufen ist harmlos.)
        var now = DateTimeOffset.UtcNow;
        var activeTokens = await dbContext.Set<RefreshToken>()
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = now;
        }

        // Neues Token-Paar fuer die aktuelle Session
        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshTokenString = tokenService.GenerateRefreshToken();
        var expiresAt = now.AddHours(tokenService.GetRefreshTokenValidityHours());

        dbContext.Set<RefreshToken>().Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = tokenService.HashRefreshToken(refreshTokenString),
            ExpiresAt = expiresAt,
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ChangePasswordResponse(accessToken, refreshTokenString, expiresAt);
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Benutzer-ID nicht gefunden.");

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Ungueltige Benutzer-ID.");
        }

        return userId;
    }
}
