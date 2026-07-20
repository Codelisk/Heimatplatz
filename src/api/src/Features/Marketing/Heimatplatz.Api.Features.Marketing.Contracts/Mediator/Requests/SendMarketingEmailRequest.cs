using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Versendet eine Marketing-E-Mail von info@heimatplatz.at an einen Kontakt.
/// Subject/Body kommen (ggf. nachbearbeitet) aus dem Generate-Schritt; die
/// professionelle Signatur mit Kontaktdaten wird serverseitig automatisch angehaengt.
/// Der Empfaenger wird in der Kontaktdatenbank angelegt bzw. aktualisiert
/// (LastContactedAt, Lead->Contacted) und der Versand in der Historie gespeichert
/// (RecipientName/Keywords dienen Kontaktanlage und Auswertung).
/// Bewusst komplett im Body (kein Route-Parameter).
/// </summary>
public record SendMarketingEmailRequest(
    string RecipientEmail,
    string Subject,
    string Body,
    string? RecipientName = null,
    string? Keywords = null
) : IRequest<SendMarketingEmailResponse>;

/// <summary>
/// Versand-Ergebnis. SmtpConfigured=false bedeutet: es ist kein SMTP-Server
/// konfiguriert (Email__SmtpHost fehlt) - die Mail wurde NUR geloggt, nicht
/// zugestellt (LoggingEmailSender). Das Web zeigt dann einen Warnhinweis.
/// ContactId verweist auf den angelegten/aktualisierten Kontakt.
/// </summary>
public record SendMarketingEmailResponse(
    bool Success,
    bool SmtpConfigured,
    string? Error,
    Guid? ContactId = null
);
