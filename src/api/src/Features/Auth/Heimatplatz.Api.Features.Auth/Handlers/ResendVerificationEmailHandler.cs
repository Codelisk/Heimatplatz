using System.IdentityModel.Tokens.Jwt;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Exceptions;
using Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Handlers;

/// <summary>
/// Handler zum erneuten Versand der Verifikations-Mail (POST /api/auth/resend-verification).
/// Erfordert Login - versendet wird immer an die eigene, im Konto hinterlegte Adresse.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class ResendVerificationEmailHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IAuthEmailService authEmailService,
    ILogger<ResendVerificationEmailHandler> logger
) : IRequestHandler<ResendVerificationEmailRequest, ResendVerificationEmailResponse>
{
    [MediatorHttpPost("/api/auth/resend-verification", OperationId = "ResendVerificationEmail", RequiresAuthorization = true)]
    public async Task<ResendVerificationEmailResponse> Handle(ResendVerificationEmailRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();

        var user = await dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Benutzer nicht gefunden.");

        if (user.EmailVerifiedAt is not null)
        {
            return new ResendVerificationEmailResponse(AlreadyVerified: true);
        }

        try
        {
            await authEmailService.SendVerificationEmailAsync(user, cancellationToken);
        }
        catch (Exception ex)
        {
            // Expliziter Neu-Versand: hier will der Benutzer die Mail JETZT - Fehler sauber melden
            logger.LogError(ex, "Verifikations-Mail an {Email} konnte nicht versendet werden.", user.Email);
            throw new ServiceUnavailableException(
                "Die Bestätigungs-E-Mail konnte gerade nicht versendet werden. Bitte versuchen Sie es später erneut.");
        }

        return new ResendVerificationEmailResponse();
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
