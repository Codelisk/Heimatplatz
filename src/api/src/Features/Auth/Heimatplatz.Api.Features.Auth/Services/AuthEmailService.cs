using System.Net;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Email;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shiny;

namespace Heimatplatz.Api.Features.Auth.Services;

/// <summary>
/// Baut und versendet die Auth-Mails. Pro Versand wird ein frischer Einmal-Token erzeugt;
/// aeltere, noch nicht eingeloeste Tokens desselben Zwecks werden geloescht (es gilt immer
/// nur der juengste Link).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class AuthEmailService(
    AppDbContext dbContext,
    IEmailSender emailSender,
    IOptions<EmailOptions> emailOptions
) : IAuthEmailService
{
    public async Task SendVerificationEmailAsync(User user, CancellationToken cancellationToken = default)
    {
        var link = await IssueTokenLinkAsync(
            user.Id,
            UserTokenPurpose.EmailVerification,
            UserActionTokens.EmailVerificationValidity,
            "email-bestaetigen",
            cancellationToken);

        var name = WebUtility.HtmlEncode(user.FirstName);
        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:560px;margin:0 auto;color:#1f1f1f">
              <h2 style="color:#b3261e">Heimatplatz</h2>
              <p>Hallo {name},</p>
              <p>willkommen bei Heimatplatz! Bitte bestätigen Sie Ihre E-Mail-Adresse, indem Sie auf den folgenden Link klicken:</p>
              <p style="margin:24px 0">
                <a href="{link}" style="background:#b3261e;color:#ffffff;padding:12px 20px;text-decoration:none;border-radius:4px">E-Mail-Adresse bestätigen</a>
              </p>
              <p>Falls der Button nicht funktioniert, öffnen Sie diesen Link in Ihrem Browser:<br>
                 <a href="{link}">{link}</a></p>
              <p>Der Link ist 3 Tage gültig.</p>
              <p style="color:#6b6b6b;font-size:13px">Sie haben sich nicht bei Heimatplatz registriert? Dann können Sie diese E-Mail einfach ignorieren.</p>
            </div>
            """;
        var text = $"""
            Hallo {user.FirstName},

            willkommen bei Heimatplatz! Bitte bestätigen Sie Ihre E-Mail-Adresse über diesen Link:

            {link}

            Der Link ist 3 Tage gültig.

            Sie haben sich nicht bei Heimatplatz registriert? Dann können Sie diese E-Mail einfach ignorieren.
            """;

        await emailSender.SendAsync(
            new EmailMessage(user.Email, "Bitte bestätigen Sie Ihre E-Mail-Adresse", html, text),
            cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(User user, CancellationToken cancellationToken = default)
    {
        var link = await IssueTokenLinkAsync(
            user.Id,
            UserTokenPurpose.PasswordReset,
            UserActionTokens.PasswordResetValidity,
            "passwort-zuruecksetzen",
            cancellationToken,
            user.Email);

        var name = WebUtility.HtmlEncode(user.FirstName);
        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:560px;margin:0 auto;color:#1f1f1f">
              <h2 style="color:#b3261e">Heimatplatz</h2>
              <p>Hallo {name},</p>
              <p>Sie haben angefordert, Ihr Passwort zurückzusetzen. Klicken Sie auf den folgenden Link, um ein neues Passwort zu wählen:</p>
              <p style="margin:24px 0">
                <a href="{link}" style="background:#b3261e;color:#ffffff;padding:12px 20px;text-decoration:none;border-radius:4px">Neues Passwort wählen</a>
              </p>
              <p>Falls der Button nicht funktioniert, öffnen Sie diesen Link in Ihrem Browser:<br>
                 <a href="{link}">{link}</a></p>
              <p>Der Link ist 2 Stunden gültig.</p>
              <p style="color:#6b6b6b;font-size:13px">Sie haben kein neues Passwort angefordert? Dann können Sie diese E-Mail ignorieren – Ihr Passwort bleibt unverändert.</p>
            </div>
            """;
        var text = $"""
            Hallo {user.FirstName},

            Sie haben angefordert, Ihr Passwort zurückzusetzen. Wählen Sie über diesen Link ein neues Passwort:

            {link}

            Der Link ist 2 Stunden gültig.

            Sie haben kein neues Passwort angefordert? Dann können Sie diese E-Mail ignorieren - Ihr Passwort bleibt unverändert.
            """;

        await emailSender.SendAsync(
            new EmailMessage(user.Email, "Passwort zurücksetzen", html, text),
            cancellationToken);
    }

    /// <summary>
    /// Loescht offene Tokens desselben Zwecks, erzeugt und speichert einen frischen Token
    /// und liefert den fertigen Frontend-Link (Klartext-Token nur im Link, Hash in der DB).
    /// </summary>
    private async Task<string> IssueTokenLinkAsync(
        Guid userId,
        UserTokenPurpose purpose,
        TimeSpan validity,
        string frontendPage,
        CancellationToken cancellationToken,
        string? passwordManagerUsername = null)
    {
        var staleTokens = await dbContext.Set<UserActionToken>()
            .Where(t => t.UserId == userId && t.Purpose == purpose && t.UsedAt == null)
            .ToListAsync(cancellationToken);
        dbContext.Set<UserActionToken>().RemoveRange(staleTokens);

        var token = UserActionTokens.GenerateToken();
        var now = DateTimeOffset.UtcNow;

        dbContext.Set<UserActionToken>().Add(new UserActionToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = UserActionTokens.HashToken(token),
            Purpose = purpose,
            ExpiresAt = now.Add(validity),
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var baseUrl = emailOptions.Value.FrontendBaseUrl.TrimEnd('/');
        var link = $"{baseUrl}/{frontendPage}/?token={token}";

        // Passwortmanager brauchen beim Setzen eines neuen Passworts den
        // zugehoerigen Benutzernamen. Das URL-Fragment erreicht nur den Browser:
        // E-Mail-Adresse weder an den Webserver noch ueber den Referrer senden.
        return string.IsNullOrWhiteSpace(passwordManagerUsername)
            ? link
            : $"{link}#email={Uri.EscapeDataString(passwordManagerUsername.Trim())}";
    }
}
