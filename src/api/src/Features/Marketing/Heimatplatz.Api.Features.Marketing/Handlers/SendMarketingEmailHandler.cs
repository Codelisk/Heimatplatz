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
/// Versendet den (ggf. nachbearbeiteten) E-Mail-Entwurf von info@heimatplatz.at an
/// den Kontakt. Die Signatur mit den Impressum-Kontaktdaten wird serverseitig
/// angehaengt - der Text aus dem Request ist nur der Fliesstext-Body.
/// Nach dem Versand wird der Empfaenger in der Kontaktdatenbank angelegt bzw.
/// aktualisiert (LastContactedAt, Lead->Contacted) und der Versand samt Message-Id
/// in der Historie gespeichert (Reply-Zuordnung des Posteingang-Syncs).
/// X-Admin-Key-Schutz wie alle /api/admin-Endpoints.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class SendMarketingEmailHandler(
    IAdminAccessGuard accessGuard,
    IMarketingEmailComposer composer,
    IEmailSender emailSender,
    AppDbContext dbContext,
    IOptions<EmailOptions> emailOptions,
    ILogger<SendMarketingEmailHandler> logger
) : IRequestHandler<SendMarketingEmailRequest, SendMarketingEmailResponse>
{
    [MediatorHttpPost("/email/send", OperationId = "SendAdminMarketingEmail")]
    public async Task<SendMarketingEmailResponse> Handle(SendMarketingEmailRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var smtpConfigured = emailOptions.Value.IsConfigured;

        if (!MailAddress.TryCreate(request.RecipientEmail?.Trim(), out var recipient))
            return new SendMarketingEmailResponse(false, smtpConfigured, "Die Empfänger-E-Mail-Adresse ist ungültig.");
        if (string.IsNullOrWhiteSpace(request.Subject))
            return new SendMarketingEmailResponse(false, smtpConfigured, "Der Betreff darf nicht leer sein.");
        if (string.IsNullOrWhiteSpace(request.Body))
            return new SendMarketingEmailResponse(false, smtpConfigured, "Der E-Mail-Text darf nicht leer sein.");

        string? ccAddress = null;
        if (!string.IsNullOrWhiteSpace(request.CcEmail))
        {
            if (!MailAddress.TryCreate(request.CcEmail.Trim(), out var cc))
                return new SendMarketingEmailResponse(false, smtpConfigured, "Die CC-E-Mail-Adresse ist ungültig.");
            ccAddress = cc.Address;
        }

        string? bccAddress = null;
        if (!string.IsNullOrWhiteSpace(request.BccEmail))
        {
            if (!MailAddress.TryCreate(request.BccEmail.Trim(), out var bcc))
                return new SendMarketingEmailResponse(false, smtpConfigured, "Die BCC-E-Mail-Adresse ist ungültig.");
            bccAddress = bcc.Address;
        }

        try
        {
            var message = await composer.ComposeAsync(recipient.Address, request.Subject, request.Body, ccAddress, bccAddress, cancellationToken);
            var result = await emailSender.SendAsync(message, cancellationToken);

            var contactId = await RecordSendAsync(recipient.Address, request, result, cancellationToken);

            logger.LogInformation("[Marketing] Marketing-E-Mail an {Recipient} versendet (Delivered={Delivered})",
                recipient.Address, result.Delivered);
            return new SendMarketingEmailResponse(true, result.Delivered, null, contactId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fehlertext bewusst durchreichen: Aufrufer ist ausschliesslich der
            // Admin-Key-authentifizierte Astro-SSR-Server des Intern-Bereichs.
            logger.LogError(ex, "[Marketing] Versand der Marketing-E-Mail an {Recipient} fehlgeschlagen", request.RecipientEmail);
            return new SendMarketingEmailResponse(false, smtpConfigured, ex.Message);
        }
    }

    /// <summary>
    /// Kontakt-Upsert + Historien-Zeile. Automatische Status-Uebergaenge beschraenken
    /// sich auf Lead->Contacted; manuell gesetzte Status (Interested, Customer, ...)
    /// bleiben unangetastet.
    /// </summary>
    private async Task<Guid> RecordSendAsync(
        string recipientAddress,
        SendMarketingEmailRequest request,
        EmailSendResult result,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = recipientAddress.Trim().ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;

        var contact = await dbContext.Set<MarketingContact>()
            .FirstOrDefaultAsync(c => c.Email == normalizedEmail, cancellationToken);

        if (contact is null)
        {
            contact = new MarketingContact
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                Name = string.IsNullOrWhiteSpace(request.RecipientName) ? null : request.RecipientName.Trim(),
                Status = MarketingContactStatus.Contacted,
                Source = "Versand"
            };
            dbContext.Set<MarketingContact>().Add(contact);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(contact.Name) && !string.IsNullOrWhiteSpace(request.RecipientName))
                contact.Name = request.RecipientName.Trim();
            if (contact.Status == MarketingContactStatus.Lead)
                contact.Status = MarketingContactStatus.Contacted;
        }

        contact.LastContactedAt = now;

        dbContext.Set<MarketingEmail>().Add(new MarketingEmail
        {
            Id = Guid.NewGuid(),
            ContactId = contact.Id,
            Subject = request.Subject.Trim(),
            Body = request.Body.Trim(),
            Keywords = string.IsNullOrWhiteSpace(request.Keywords) ? null : request.Keywords.Trim(),
            MessageId = result.MessageId,
            Status = result.Delivered ? MarketingEmailStatus.Sent : MarketingEmailStatus.LoggedOnly,
            SentAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return contact.Id;
    }
}
