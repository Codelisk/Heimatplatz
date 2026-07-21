using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Heimatplatz.Api.Core.Email;

/// <summary>
/// SMTP-Versand via MailKit (System.Net.Mail.SmtpClient ist obsolet und von Microsoft
/// selbst zugunsten MailKit abgekuendigt). Pro Mail eine frische Verbindung - bei den
/// geringen Volumina (Verifikation, Passwort-Reset) ist Connection-Pooling unnoetig.
/// </summary>
public class SmtpEmailSender(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailSender> logger
) : IEmailSender
{
    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(opts.FromName, opts.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.ToAddress));
        mime.Subject = message.Subject;

        // Message-Id vor dem Versand fixieren: geht so auf die Leitung und taucht in
        // Antworten als In-Reply-To wieder auf (Reply-Zuordnung im Marketing-Posteingang).
        var messageId = mime.MessageId ?? MimeKit.Utils.MimeUtils.GenerateMessageId();
        mime.MessageId = messageId;
        mime.Body = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody
        }.ToMessageBody();

        using var client = new SmtpClient();
        client.Timeout = opts.TimeoutSeconds * 1000;

        // 465 = implizites TLS ab Verbindungsaufbau, sonst (587) STARTTLS erzwingen -
        // niemals unverschluesselt, es gehen Zugangsdaten ueber die Leitung.
        var socketOptions = opts.SmtpPort == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        await client.ConnectAsync(opts.SmtpHost, opts.SmtpPort, socketOptions, cancellationToken);
        await client.AuthenticateAsync(opts.SmtpUsername, opts.SmtpPassword, cancellationToken);
        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        logger.LogInformation("E-Mail \"{Subject}\" an {To} versendet.", message.Subject, message.ToAddress);

        if (message.ArchiveToSentFolder && opts.IsImapConfigured)
            await ArchiveToSentFolderAsync(mime, opts, cancellationToken);

        return new EmailSendResult(Delivered: true, MessageId: messageId);
    }

    /// <summary>
    /// Legt die versendete Mail per IMAP-APPEND im Gesendet-Ordner ab, damit sie im
    /// Webmail sichtbar ist. Fehler hier duerfen den Versand NICHT scheitern lassen -
    /// die Mail ist bereits beim SMTP-Server, es geht nur noch um die Kopie.
    /// </summary>
    private async Task ArchiveToSentFolderAsync(MimeMessage mime, EmailOptions opts, CancellationToken cancellationToken)
    {
        try
        {
            using var imap = new ImapClient();
            imap.Timeout = opts.TimeoutSeconds * 1000;

            var socketOptions = opts.ImapPort == 993
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;
            await imap.ConnectAsync(opts.EffectiveImapHost, opts.ImapPort, socketOptions, cancellationToken);
            await imap.AuthenticateAsync(opts.SmtpUsername, opts.SmtpPassword, cancellationToken);

            var sent = ResolveSentFolder(imap);
            if (sent is null)
            {
                logger.LogWarning("Gesendet-Ordner im Postfach nicht gefunden - Kopie der Mail an {To} nicht abgelegt.", mime.To);
                return;
            }

            // Als gelesen markieren - die eigene gesendete Mail soll nicht "neu" wirken
            await sent.AppendAsync(mime, MessageFlags.Seen, cancellationToken);
            await imap.DisconnectAsync(quit: true, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Kopie in den Gesendet-Ordner fehlgeschlagen (Versand selbst war erfolgreich).");
        }
    }

    /// <summary>
    /// SPECIAL-USE-Ordner (\\Sent) bevorzugen; Fallback auf gaengige Ordnernamen,
    /// falls der Server die Erweiterung nicht anbietet.
    /// </summary>
    private static IMailFolder? ResolveSentFolder(ImapClient imap)
    {
        try
        {
            var special = imap.GetFolder(SpecialFolder.Sent);
            if (special is not null)
                return special;
        }
        catch (NotSupportedException)
        {
            // Server ohne SPECIAL-USE/XLIST - unten per Namen suchen
        }

        string[] candidates = ["Sent", "Sent Items", "Sent Messages", "Gesendet", "Gesendete Elemente"];
        var personal = imap.GetFolder(imap.PersonalNamespaces[0]);
        return personal
            .GetSubfolders(subscribedOnly: false)
            .FirstOrDefault(f => candidates.Contains(f.Name, StringComparer.OrdinalIgnoreCase));
    }
}
