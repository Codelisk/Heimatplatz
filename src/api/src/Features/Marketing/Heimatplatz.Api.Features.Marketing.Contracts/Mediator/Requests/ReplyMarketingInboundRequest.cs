using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Antwort auf eine eingegangene Rueckmeldung (Marketing-Posteingang) verfassen.
/// Der Body ist der Fliesstext OHNE Signatur - die Signatur mit den Impressum-Kontaktdaten
/// haengt der Composer serverseitig an (wie beim regulaeren Marketing-Versand).
/// Empfaenger und Betreff ("Re: ...") kommen aus der Eingangs-Mail; In-Reply-To/References
/// werden gesetzt, damit die Antwort beim Empfaenger im Thread haengt.
/// Bewusst komplett im Body (kein Route-Parameter).
/// </summary>
public record ReplyMarketingInboundRequest(
    Guid InboundEmailId,
    string Body
) : IRequest<ReplyMarketingInboundResponse>;

/// <summary>
/// Versand-Ergebnis. SmtpConfigured=false bedeutet: kein SMTP-Server konfiguriert -
/// die Mail wurde NUR geloggt, nicht zugestellt (LoggingEmailSender).
/// </summary>
public record ReplyMarketingInboundResponse(
    bool Success,
    bool SmtpConfigured,
    string? Error
);
