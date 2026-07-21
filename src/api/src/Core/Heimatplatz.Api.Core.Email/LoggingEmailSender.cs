using Microsoft.Extensions.Logging;
using MimeKit.Utils;

namespace Heimatplatz.Api.Core.Email;

/// <summary>
/// Fallback ohne SMTP-Konfiguration (lokale Entwicklung): loggt die Mail inkl. Text-Body,
/// damit Verifikations-/Reset-Links aus dem Log kopiert und getestet werden koennen.
/// </summary>
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "E-Mail-Versand nicht konfiguriert (Email__SmtpHost fehlt) - Mail wird NUR geloggt.\n" +
            "An: {To}\nCc: {Cc}\nBcc: {Bcc}\nBetreff: {Subject}\n{Body}",
            message.ToAddress, message.CcAddress ?? "-", message.BccAddress ?? "-",
            message.Subject, message.TextBody);

        // Synthetische Message-Id, damit Aufrufer (Marketing-Versand-Historie) auch im
        // Logging-Modus einen konsistenten Datensatz bekommen.
        return Task.FromResult(new EmailSendResult(Delivered: false, MessageId: MimeUtils.GenerateMessageId()));
    }
}
