namespace Heimatplatz.Api.Features.Marketing.Contracts.Models;

/// <summary>
/// E-Mail-Vorlage. <see cref="Subject"/> und <see cref="Body"/> enthalten Platzhalter
/// (siehe <see cref="MarketingTemplatePlaceholders"/>), die serverseitig aus dem Kontakt
/// befuellt werden - das Ergebnis bleibt im Editor aenderbar.
/// </summary>
public record MarketingTemplateDto(
    Guid Id,
    string Name,
    string? Description,
    string Subject,
    string Body,
    bool IsActive,
    int DisplayOrder,
    DateTimeOffset CreatedAt
);

/// <summary>
/// Unterstuetzte Platzhalter einer Vorlage - Einzige Quelle fuer Renderer und Web-Hilfetext,
/// damit beide nicht auseinanderlaufen.
/// </summary>
public static class MarketingTemplatePlaceholders
{
    public const string Salutation = "{anrede}";
    public const string Company = "{firma}";
    public const string Name = "{name}";
    public const string City = "{ort}";

    /// <summary>Alle Platzhalter in Anzeige-Reihenfolge (fuer den Hilfetext im Editor).</summary>
    public static readonly string[] All = [Salutation, Company, Name, City];
}
