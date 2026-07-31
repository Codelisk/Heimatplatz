using System.Net.Mail;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Email;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Heimatplatz.Api.Features.Marketing.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>
/// Antwort auf eine Rueckmeldung aus dem Marketing-Posteingang: versendet den Text
/// von info@heimatplatz.at als echtes Reply (In-Reply-To/References auf die
/// Eingangs-Mail), haengt die Signatur serverseitig an und speichert den Versand
/// in der Kontakt-Historie (Message-Id fuer die Zuordnung weiterer Antworten).
/// Die beantwortete Rueckmeldung gilt danach als gelesen.
/// X-Admin-Key-Schutz wie alle /api/admin-Endpoints.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class ReplyMarketingInboundHandler(
    IAdminAccessGuard accessGuard,
    IMarketingEmailComposer composer,
    IEmailSender emailSender,
    AppDbContext dbContext,
    IOptions<EmailOptions> emailOptions,
    ILogger<ReplyMarketingInboundHandler> logger
) : IRequestHandler<ReplyMarketingInboundRequest, ReplyMarketingInboundResponse>
{
    [MediatorHttpPost("/inbox/reply", OperationId = "ReplyMarketingInbound")]
    public async Task<ReplyMarketingInboundResponse> Handle(ReplyMarketingInboundRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var smtpConfigured = emailOptions.Value.IsConfigured;

        if (string.IsNullOrWhiteSpace(request.Body))
            return new ReplyMarketingInboundResponse(false, smtpConfigured, "Der Antwort-Text darf nicht leer sein.");

        var inbound = await dbContext.Set<MarketingInboundEmail>()
            .Include(i => i.Contact)
            .FirstOrDefaultAsync(i => i.Id == request.InboundEmailId, cancellationToken);
        if (inbound is null)
            return new ReplyMarketingInboundResponse(false, smtpConfigured, "Die Rückmeldung wurde nicht gefunden.");

        // Bounces kommen von mailer-daemon/postmaster - eine Antwort dorthin laeuft ins Leere
        if (inbound.IsBounce)
            return new ReplyMarketingInboundResponse(false, smtpConfigured, "Auf eine Unzustellbarkeits-Meldung kann nicht geantwortet werden.");

        if (!MailAddress.TryCreate(inbound.FromAddress, out var recipient))
            return new ReplyMarketingInboundResponse(false, smtpConfigured, "Die Absender-Adresse der Rückmeldung ist ungültig.");

        var subject = BuildReplySubject(inbound.Subject);

        try
        {
            var message = await composer.ComposeAsync(recipient.Address, subject, request.Body, ct: cancellationToken);

            // Threading-Header: References = Kette der Eingangs-Mail + deren eigene Id.
            // Der Sync speichert fuer Mails ohne Message-Id-Header einen "uid:..."-Fallback -
            // der ist keine gueltige Message-Id und bleibt deshalb draussen.
            var references = new List<string>();
            if (IsValidMessageId(inbound.InReplyTo))
                references.Add(inbound.InReplyTo!);
            string? inReplyTo = null;
            if (IsValidMessageId(inbound.MessageId))
            {
                inReplyTo = inbound.MessageId;
                references.Add(inbound.MessageId);
            }

            message = message with
            {
                InReplyToMessageId = inReplyTo,
                ReferenceMessageIds = references.Count > 0 ? references : null
            };

            var result = await emailSender.SendAsync(message, cancellationToken);

            await RecordReplyAsync(inbound, recipient.Address, subject, request.Body, result, cancellationToken);

            logger.LogInformation("[Marketing] Antwort auf Rückmeldung {InboundId} an {Recipient} versendet (Delivered={Delivered})",
                inbound.Id, recipient.Address, result.Delivered);
            return new ReplyMarketingInboundResponse(true, result.Delivered, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fehlertext bewusst durchreichen: Aufrufer ist ausschliesslich der
            // Admin-Key-authentifizierte Astro-SSR-Server des Intern-Bereichs.
            logger.LogError(ex, "[Marketing] Antwort auf Rückmeldung {InboundId} fehlgeschlagen", request.InboundEmailId);
            return new ReplyMarketingInboundResponse(false, smtpConfigured, ex.Message);
        }
    }

    /// <summary>
    /// Versand-Historie + Kontakt-Pflege wie beim regulaeren Marketing-Versand
    /// (SendMarketingEmailHandler.RecordSendAsync), nur ohne Neuanlage-Sonderfaelle:
    /// Der Kontakt haengt in aller Regel bereits an der Eingangs-Mail; fehlt er
    /// (geloescht), wird er ueber die Absender-Adresse gefunden oder neu angelegt.
    /// </summary>
    private async Task RecordReplyAsync(
        MarketingInboundEmail inbound,
        string recipientAddress,
        string subject,
        string body,
        EmailSendResult result,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = recipientAddress.Trim().ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;

        var contact = inbound.Contact
            ?? await dbContext.Set<MarketingContact>().FirstOrDefaultAsync(
                c => c.Email == normalizedEmail || c.AdditionalEmails.Any(a => a.Email == normalizedEmail),
                cancellationToken);

        if (contact is null)
        {
            contact = new MarketingContact
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                Name = string.IsNullOrWhiteSpace(inbound.FromName) ? null : inbound.FromName.Trim(),
                Status = MarketingContactStatus.Contacted,
                Source = "Versand"
            };
            dbContext.Set<MarketingContact>().Add(contact);
            inbound.ContactId = contact.Id;
        }
        else if (contact.Status is MarketingContactStatus.Lead or MarketingContactStatus.ToContact)
        {
            contact.Status = MarketingContactStatus.Contacted;
        }

        contact.LastContactedAt = now;

        // Beantwortet impliziert gelesen - die Rueckmeldung soll nicht "neu" bleiben
        inbound.IsRead = true;

        dbContext.Set<MarketingEmail>().Add(new MarketingEmail
        {
            Id = Guid.NewGuid(),
            ContactId = contact.Id,
            Subject = subject,
            Body = body.Trim(),
            MessageId = result.MessageId,
            Status = result.Delivered ? MarketingEmailStatus.Sent : MarketingEmailStatus.LoggedOnly,
            SentAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>"Re: " voranstellen, aber vorhandene Re:/AW:-Praefixe nicht stapeln.</summary>
    private static string BuildReplySubject(string? subject)
    {
        var trimmed = subject?.Trim() ?? "";
        if (trimmed.Length == 0)
            return "Re: Ihre Nachricht";
        if (trimmed.StartsWith("Re:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("AW:", StringComparison.OrdinalIgnoreCase))
            return trimmed;
        return $"Re: {trimmed}";
    }

    /// <summary>Echte Message-Id (Posteingang-Sync speichert sonst einen "uid:..."-Fallback).</summary>
    private static bool IsValidMessageId(string? messageId) =>
        !string.IsNullOrWhiteSpace(messageId) && messageId.Contains('@') && !messageId.StartsWith("uid:", StringComparison.Ordinal);
}
