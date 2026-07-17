using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Handlers;

/// <summary>
/// Handler fuer "Passwort vergessen" (POST /api/auth/forgot-password).
/// Antwortet IMMER mit derselben generischen Meldung - ob ein Konto existiert, darf
/// nicht erkennbar sein (User Enumeration). Auch Mail-Versand-Fehler werden deshalb
/// nur geloggt, nie an den Client gemeldet.
/// </summary>
[AllowAnonymous]
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class ForgotPasswordHandler(
    AppDbContext dbContext,
    IAuthEmailService authEmailService,
    ILogger<ForgotPasswordHandler> logger
) : IRequestHandler<ForgotPasswordRequest, ForgotPasswordResponse>
{
    [MediatorHttpPost("/api/auth/forgot-password", OperationId = "ForgotPassword")]
    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        // Format-Validierung leakt nichts ueber existierende Konten
        var email = UserInputValidator.NormalizeAndValidateEmail(request.Email);

        var user = await dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is not null)
        {
            try
            {
                await authEmailService.SendPasswordResetEmailAsync(user, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Passwort-Reset-Mail an {Email} konnte nicht versendet werden.", user.Email);
            }
        }

        return new ForgotPasswordResponse(
            "Falls ein Konto mit dieser E-Mail-Adresse existiert, haben wir Ihnen einen Link zum Zurücksetzen des Passworts gesendet.");
    }
}
