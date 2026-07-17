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
/// Handler zum Bestaetigen der E-Mail-Adresse (POST /api/auth/verify-email).
/// Anonym aufrufbar: der Link aus der Mail wird haeufig in einem Browser ohne Session
/// geoeffnet - der Besitz des Einmal-Tokens IST der Nachweis.
/// </summary>
[AllowAnonymous]
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class VerifyEmailHandler(
    AppDbContext dbContext
) : IRequestHandler<VerifyEmailRequest, VerifyEmailResponse>
{
    [MediatorHttpPost("/api/auth/verify-email", OperationId = "VerifyEmail")]
    public async Task<VerifyEmailResponse> Handle(VerifyEmailRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var token = request.Token?.Trim();
        if (string.IsNullOrEmpty(token))
        {
            throw new ValidationException("Der Bestätigungslink ist unvollständig (Token fehlt).");
        }

        var tokenHash = UserActionTokens.HashToken(token);
        var actionToken = await dbContext.Set<UserActionToken>()
            .Include(t => t.User)
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash && t.Purpose == UserTokenPurpose.EmailVerification,
                cancellationToken);

        // Ablauf/Einloesung in-memory pruefen (DateTimeOffset-Vergleiche sind auf SQLite
        // nicht uebersetzbar) - bei einem einzelnen Token ist das ohnehin egal.
        if (actionToken?.User is null
            || actionToken.UsedAt is not null
            || actionToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new ValidationException(
                "Der Bestätigungslink ist ungültig oder abgelaufen. " +
                "Sie können in Ihrem Profil eine neue Bestätigungs-E-Mail anfordern.");
        }

        var user = actionToken.User;
        var alreadyVerified = user.EmailVerifiedAt is not null;
        var now = DateTimeOffset.UtcNow;

        actionToken.UsedAt = now;
        if (!alreadyVerified)
        {
            user.EmailVerifiedAt = now;
            user.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new VerifyEmailResponse(user.Email, alreadyVerified);
    }
}
