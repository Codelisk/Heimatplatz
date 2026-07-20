using System.Net.Mail;
using Heimatplatz.Api.Core.Email;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>
/// Versendet den (ggf. nachbearbeiteten) E-Mail-Entwurf von info@heimatplatz.at an
/// den Kontakt. Die Signatur mit den Impressum-Kontaktdaten wird serverseitig
/// angehaengt - der Text aus dem Request ist nur der Fliesstext-Body.
/// X-Admin-Key-Schutz wie alle /api/admin-Endpoints.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class SendMarketingEmailHandler(
    IAdminAccessGuard accessGuard,
    IMarketingEmailComposer composer,
    IEmailSender emailSender,
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

        try
        {
            var message = await composer.ComposeAsync(recipient.Address, request.Subject, request.Body, cancellationToken);
            await emailSender.SendAsync(message, cancellationToken);

            logger.LogInformation("[Marketing] Marketing-E-Mail an {Recipient} versendet (SmtpConfigured={SmtpConfigured})",
                recipient.Address, smtpConfigured);
            return new SendMarketingEmailResponse(true, smtpConfigured, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fehlertext bewusst durchreichen: Aufrufer ist ausschliesslich der
            // Admin-Key-authentifizierte Astro-SSR-Server des Intern-Bereichs.
            logger.LogError(ex, "[Marketing] Versand der Marketing-E-Mail an {Recipient} fehlgeschlagen", request.RecipientEmail);
            return new SendMarketingEmailResponse(false, smtpConfigured, ex.Message);
        }
    }
}
