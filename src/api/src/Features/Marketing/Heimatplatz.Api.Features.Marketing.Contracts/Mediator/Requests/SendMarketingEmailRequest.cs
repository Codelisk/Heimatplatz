using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Versendet eine Marketing-E-Mail von info@heimatplatz.at an einen Kontakt.
/// Subject/Body kommen (ggf. nachbearbeitet) aus dem Generate-Schritt; die
/// professionelle Signatur mit Kontaktdaten wird serverseitig automatisch angehaengt.
/// Bewusst komplett im Body (kein Route-Parameter).
/// </summary>
public record SendMarketingEmailRequest(
    string RecipientEmail,
    string Subject,
    string Body
) : IRequest<SendMarketingEmailResponse>;

/// <summary>
/// Versand-Ergebnis. SmtpConfigured=false bedeutet: es ist kein SMTP-Server
/// konfiguriert (Email__SmtpHost fehlt) - die Mail wurde NUR geloggt, nicht
/// zugestellt (LoggingEmailSender). Das Web zeigt dann einen Warnhinweis.
/// </summary>
public record SendMarketingEmailResponse(
    bool Success,
    bool SmtpConfigured,
    string? Error
);
