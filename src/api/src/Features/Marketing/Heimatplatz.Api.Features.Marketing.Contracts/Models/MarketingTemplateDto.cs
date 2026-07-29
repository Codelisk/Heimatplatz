using System.Text.RegularExpressions;

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
/// Unterstuetzte Platzhalter einer Vorlage - Einzige Quelle fuer Renderer, Validierung
/// und Web-Hilfetext, damit sie nicht auseinanderlaufen. Die Menge ist bewusst
/// geschlossen: Vorlagen mit unbekannten Platzhaltern werden beim Speichern abgelehnt,
/// im Entwurf verbliebene Platzhalter blockieren den Versand (fail-closed statt
/// stillschweigend kaputter Kundenmail).
/// </summary>
public static class MarketingTemplatePlaceholders
{
    public const string Salutation = "{anrede}";
    public const string Company = "{firma}";
    public const string Name = "{name}";
    public const string City = "{ort}";

    /// <summary>Alle Platzhalter in Anzeige-Reihenfolge (fuer den Hilfetext im Editor).</summary>
    public static readonly string[] All = [Salutation, Company, Name, City];

    /// <summary>
    /// Findet alle Platzhalter-Tokens ({wort}, nur Buchstaben, Gross-/Kleinschreibung und
    /// Leerraum innerhalb der Klammern egal) in einem Text. Absichtlich eng gefasst:
    /// "{1}" oder "{a b}" sind keine Tokens und bleiben unangetastet.
    /// </summary>
    public static IReadOnlyList<PlaceholderToken> FindTokens(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        return TokenRegex.Matches(text)
            .Select(m =>
            {
                var normalized = "{" + m.Groups[1].Value.ToLowerInvariant() + "}";
                return new PlaceholderToken(m.Value, normalized, All.Contains(normalized));
            })
            .ToList();
    }

    /// <summary>
    /// Ersetzt alle Platzhalter-Tokens in einem Durchgang. <paramref name="resolve"/>
    /// liefert den Ersatzwert oder null = Token bleibt wie getippt stehen. Ersatzwerte
    /// werden nicht erneut gescannt - ein "{ort}" im Firmennamen bliebe Literal.
    /// </summary>
    public static string ReplaceTokens(string text, Func<PlaceholderToken, string?> resolve)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        return TokenRegex.Replace(text, m =>
        {
            var normalized = "{" + m.Groups[1].Value.ToLowerInvariant() + "}";
            var token = new PlaceholderToken(m.Value, normalized, All.Contains(normalized));
            return resolve(token) ?? m.Value;
        });
    }

    private static readonly Regex TokenRegex = new(
        @"\{\s*([a-zA-ZÄÖÜäöüß]{2,40})\s*\}",
        RegexOptions.Compiled);
}

/// <summary>
/// Ein im Text gefundenes Platzhalter-Token. <see cref="Raw"/> = Fundstelle wie getippt
/// (z.B. "{ Anrede }"), <see cref="Normalized"/> = kanonische Form ("{anrede}").
/// </summary>
public record PlaceholderToken(string Raw, string Normalized, bool IsKnown);
