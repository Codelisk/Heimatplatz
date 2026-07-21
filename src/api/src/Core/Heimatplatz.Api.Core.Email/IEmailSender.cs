namespace Heimatplatz.Api.Core.Email;

/// <summary>
/// Eine ausgehende E-Mail. HtmlBody ist die primaere Darstellung, TextBody die
/// Plain-Text-Alternative fuer Clients ohne HTML-Rendering (Multipart/Alternative).
/// ArchiveToSentFolder=true legt nach erfolgreichem SMTP-Versand zusaetzlich eine
/// Kopie per IMAP im Gesendet-Ordner des Postfachs ab (SMTP selbst tut das nicht -
/// normalerweise macht das der Mail-Client). Genutzt von Marketing-Mails, damit sie
/// im Webmail auftauchen; Auth-Mails bleiben bewusst ohne (kein IMAP-Roundtrip im
/// Registrierungs-Flow).
/// </summary>
public record EmailMessage(
    string ToAddress,
    string Subject,
    string HtmlBody,
    string TextBody,
    bool ArchiveToSentFolder = false
);

/// <summary>
/// Ergebnis eines Versands. MessageId ist die SMTP-Message-Id der Mail (ohne spitze
/// Klammern) - Aufrufer koennen sie persistieren, um spaetere Antworten ueber den
/// In-Reply-To-Header zuzuordnen (Marketing-Posteingang). Delivered=false bedeutet:
/// kein SMTP konfiguriert, die Mail wurde nur geloggt (LoggingEmailSender).
/// </summary>
public record EmailSendResult(bool Delivered, string MessageId);

/// <summary>
/// Abstraktion fuer den E-Mail-Versand. Implementierungen: SmtpEmailSender (produktiv),
/// LoggingEmailSender (Fallback ohne SMTP-Konfiguration, loggt nur).
/// </summary>
public interface IEmailSender
{
    /// <summary>Versendet die Mail; wirft bei Versand-Fehlern (Aufrufer entscheidet, ob das fatal ist).</summary>
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
