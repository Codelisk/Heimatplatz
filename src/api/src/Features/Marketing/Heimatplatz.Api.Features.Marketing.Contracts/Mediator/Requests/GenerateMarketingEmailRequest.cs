using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Erzeugt einen E-Mail-Entwurf (Betreff + Text) aus Stichwoertern ueber den
/// konfigurierten Provider (Mock/AiConnector). Der Empfaenger-Name ist optional und
/// dient nur der Anrede - die E-Mail-Adresse des Kontakts wird bewusst NICHT an die
/// KI uebergeben (erst beim Versand verwendet).
/// Bewusst komplett im Body (kein Route-Parameter), damit das Binding des generierten
/// Endpoints eindeutig ist.
/// </summary>
public record GenerateMarketingEmailRequest(
    string Keywords,
    string? RecipientName
) : IRequest<GenerateMarketingEmailResponse>;

/// <summary>
/// Entwurf-Ergebnis. Bei Fehlern (KI nicht erreichbar, ungueltige Antwort) ist
/// Success=false und Error enthaelt die Ursache fuer die Anzeige im Intern-Bereich.
/// SignatureText ist die Plaintext-Vorschau der Signatur, die beim Versand
/// automatisch angehaengt wird (im Web nur Anzeige, nicht editierbar).
/// </summary>
public record GenerateMarketingEmailResponse(
    bool Success,
    string? Subject,
    string? Body,
    string? SignatureText,
    string? Error
);
