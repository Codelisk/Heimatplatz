using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Exceptions;
using Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Handlers;

/// <summary>
/// Handler zum Setzen eines neuen Passworts via Reset-Token (POST /api/auth/reset-password).
/// Widerruft alle bestehenden Refresh Tokens (ein kompromittiertes Konto wird damit ueberall
/// abgemeldet). Ein erfolgreicher Reset beweist Postfach-Besitz und bestaetigt die E-Mail mit.
/// </summary>
[AllowAnonymous]
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class ResetPasswordHandler(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher
) : IRequestHandler<ResetPasswordRequest, ResetPasswordResponse>
{
    [MediatorHttpPost("/api/auth/reset-password", OperationId = "ResetPassword")]
    public async Task<ResetPasswordResponse> Handle(ResetPasswordRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var token = request.Token?.Trim();
        if (string.IsNullOrEmpty(token))
        {
            throw new ValidationException("Der Link ist unvollständig (Token fehlt).");
        }

        var newPassword = UserInputValidator.ValidatePassword(request.NewPassword);

        var tokenHash = UserActionTokens.HashToken(token);
        var actionToken = await dbContext.Set<UserActionToken>()
            .Include(t => t.User)
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash && t.Purpose == UserTokenPurpose.PasswordReset,
                cancellationToken);

        // Ablauf/Einloesung in-memory pruefen (DateTimeOffset-Vergleiche sind auf SQLite
        // nicht uebersetzbar)
        if (actionToken?.User is null
            || actionToken.UsedAt is not null
            || actionToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new ValidationException(
                "Der Link zum Zurücksetzen ist ungültig oder abgelaufen. " +
                "Bitte fordern Sie über \"Passwort vergessen\" einen neuen an.");
        }

        var user = actionToken.User;
        var now = DateTimeOffset.UtcNow;

        user.PasswordHash = passwordHasher.Hash(newPassword);
        user.UpdatedAt = now;
        // Der Reset kam aus einer Mail an dieses Postfach - das bestaetigt die Adresse gleich mit
        user.EmailVerifiedAt ??= now;
        actionToken.UsedAt = now;

        // Alle Refresh Tokens widerrufen - gestohlene/alte Sessions enden hier.
        // (Ohne Ablauf-Filter, siehe ChangePasswordHandler: abgelaufene zusaetzlich
        // zu widerrufen ist harmlos.)
        var activeTokens = await dbContext.Set<RefreshToken>()
            .Where(t => t.UserId == user.Id && !t.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var refreshToken in activeTokens)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ResetPasswordResponse(
            "Ihr Passwort wurde geändert. Sie können sich jetzt mit dem neuen Passwort anmelden.");
    }
}
