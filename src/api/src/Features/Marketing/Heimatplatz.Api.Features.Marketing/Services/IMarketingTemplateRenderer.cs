using Heimatplatz.Api.Features.Marketing.Data.Entities;

namespace Heimatplatz.Api.Features.Marketing.Services;

/// <summary>
/// Gerenderter Vorlagen-Entwurf (ohne Signatur - die haengt der Composer an).
/// <see cref="Warnings"/> listet alles, was nicht sauber aufging: nicht befuellbare
/// Platzhalter (bleiben sichtbar im Text stehen), Anrede-Fallback, unbekannte Tokens.
/// </summary>
public record MarketingRenderedTemplate(string Subject, string Body, IReadOnlyList<string> Warnings);

/// <summary>
/// Fuellt die Platzhalter einer E-Mail-Vorlage aus einem Kontakt. Bewusst serverseitig,
/// damit die Anrede-Regel nur an einer Stelle steht (Backend-First).
/// </summary>
public interface IMarketingTemplateRenderer
{
    /// <summary>
    /// Ersetzt alle bekannten Platzhalter. <paramref name="contact"/> = null liefert eine
    /// neutrale Vorschau. Platzhalter ohne Wert und unbekannte Platzhalter bleiben stehen
    /// und erscheinen in den Warnings - stumm leere Luecken gibt es nicht.
    /// </summary>
    MarketingRenderedTemplate Render(MarketingEmailTemplate template, MarketingContact? contact);
}
