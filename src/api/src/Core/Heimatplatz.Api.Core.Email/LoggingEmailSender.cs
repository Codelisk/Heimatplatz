using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Core.Email;

/// <summary>
/// Fallback ohne SMTP-Konfiguration (lokale Entwicklung): loggt die Mail inkl. Text-Body,
/// damit Verifikations-/Reset-Links aus dem Log kopiert und getestet werden koennen.
/// </summary>
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "E-Mail-Versand nicht konfiguriert (Email__SmtpHost fehlt) - Mail wird NUR geloggt.\n" +
            "An: {To}\nBetreff: {Subject}\n{Body}",
            message.ToAddress, message.Subject, message.TextBody);

        return Task.CompletedTask;
    }
}
