using Heimatplatz.Api.Features.Marketing.Data.Entities;

namespace Heimatplatz.Api.Features.Marketing.Services;

/// <summary>Gerenderter Vorlagen-Entwurf (ohne Signatur - die haengt der Composer an).</summary>
public record MarketingRenderedTemplate(string Subject, string Body);

/// <summary>
/// Fuellt die Platzhalter einer E-Mail-Vorlage aus einem Kontakt. Bewusst serverseitig,
/// damit die Anrede-Regel nur an einer Stelle steht (Backend-First).
/// </summary>
public interface IMarketingTemplateRenderer
{
    /// <summary>
    /// Ersetzt alle bekannten Platzhalter. <paramref name="contact"/> = null liefert eine
    /// neutrale Vorschau. Unbekannte Platzhalter bleiben stehen, damit sie im Editor auffallen.
    /// </summary>
    MarketingRenderedTemplate Render(MarketingEmailTemplate template, MarketingContact? contact);
}
