using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Liefert die Plaintext-Vorschau der Signatur, die beim Versand automatisch
/// angehaengt wird. Fuer den Intern-Bereich, damit die Compose-Seite die Signatur
/// auch ohne KI-Generierung anzeigen kann (selbst geschriebene E-Mails).
/// </summary>
public record GetMarketingEmailSignatureRequest : IRequest<GetMarketingEmailSignatureResponse>;

/// <summary>Signatur-Vorschau (Quelle: aktives Impressum, siehe MarketingEmailComposer).</summary>
public record GetMarketingEmailSignatureResponse(string SignatureText);
