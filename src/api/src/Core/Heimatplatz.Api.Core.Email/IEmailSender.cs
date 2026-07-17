namespace Heimatplatz.Api.Core.Email;

/// <summary>
/// Eine ausgehende E-Mail. HtmlBody ist die primaere Darstellung, TextBody die
/// Plain-Text-Alternative fuer Clients ohne HTML-Rendering (Multipart/Alternative).
/// </summary>
public record EmailMessage(
    string ToAddress,
    string Subject,
    string HtmlBody,
    string TextBody
);

/// <summary>
/// Abstraktion fuer den E-Mail-Versand. Implementierungen: SmtpEmailSender (produktiv),
/// LoggingEmailSender (Fallback ohne SMTP-Konfiguration, loggt nur).
/// </summary>
public interface IEmailSender
{
    /// <summary>Versendet die Mail; wirft bei Versand-Fehlern (Aufrufer entscheidet, ob das fatal ist).</summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
