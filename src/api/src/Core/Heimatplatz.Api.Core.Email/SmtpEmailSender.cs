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

        return new EmailSendResult(Delivered: true, MessageId: messageId);
    }
}
