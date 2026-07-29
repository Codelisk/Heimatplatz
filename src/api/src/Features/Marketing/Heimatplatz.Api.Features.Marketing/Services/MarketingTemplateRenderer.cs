using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Shiny;

namespace Heimatplatz.Api.Features.Marketing.Services;

/// <summary>
/// Platzhalter-Ersetzung fuer E-Mail-Vorlagen - deterministisch und fail-closed:
///
/// {anrede} wird aus den strukturierten Kontaktfeldern gebaut (nie geraten, nie leer):
/// - Nachname + Herr/Frau  -> "Sehr geehrter Herr Mag. Kaindl" / "Sehr geehrte Frau ..."
/// - Nachname ohne Anrede  -> "Guten Tag [Titel] [Vorname] Nachname" (Geschlecht unbekannt,
///   z.B. Firmenbuch-Kontakte - die geschlechtsneutrale Form ist immer korrekt)
/// - nur Alt-Name          -> gleiche Regeln mit dem vollen Namen
/// - keine Ansprechperson  -> "Sehr geehrte Damen und Herren" (+ Warning)
///
/// {name}/{firma}/{ort} ohne Wert werden NICHT stumm durch Leerstring ersetzt - der
/// Platzhalter bleibt sichtbar im Text stehen und landet in den Warnings. Der Versand
/// blockiert verbliebene Platzhalter serverseitig (SendMarketingEmailHandler), damit nie
/// eine Mail mit "{firma}" oder abgehacktem Satz rausgeht.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class MarketingTemplateRenderer : IMarketingTemplateRenderer
{
    internal const string FallbackSalutation = "Sehr geehrte Damen und Herren";

    public MarketingRenderedTemplate Render(MarketingEmailTemplate template, MarketingContact? contact)
    {
        // Reihenfolge bleibt Fundreihenfolge, Doppelmeldungen (gleicher Platzhalter in
        // Betreff und Text) erscheinen nur einmal
        var warnings = new List<string>();

        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [MarketingTemplatePlaceholders.Salutation] = BuildSalutation(contact, warnings),
            [MarketingTemplatePlaceholders.Name] = BuildDisplayName(contact),
            [MarketingTemplatePlaceholders.Company] = Trimmed(contact?.Company),
            [MarketingTemplatePlaceholders.City] = Trimmed(contact?.City)
        };

        var subject = ReplaceTokens(template.Subject, values, contact, warnings);
        var body = ReplaceTokens(template.Body, values, contact, warnings);

        return new MarketingRenderedTemplate(subject, body, warnings);
    }

    /// <summary>Anzeigename fuer {name}: strukturierte Teile vor Alt-Bestand.</summary>
    internal static string? BuildDisplayName(MarketingContact? contact)
        => JoinNonEmpty(contact?.Title, contact?.FirstName, contact?.LastName)
           ?? Trimmed(contact?.Name);

    private static string BuildSalutation(MarketingContact? contact, List<string> warnings)
    {
        // Nachname zuerst, Alt-Name als Fallback: "Sehr geehrter Herr Mag. Kaindl" braucht
        // den Nachnamen - mit Alt-Name wird es "Sehr geehrter Herr Mag. Thomas Kaindl"
        // (laenger, aber nie falsch)
        var formalName = JoinNonEmpty(contact?.Title, contact?.LastName)
            ?? Trimmed(contact?.Name);

        if (formalName is null)
        {
            warnings.Add(contact is null
                ? "Anrede: Kein Kontakt verknüpft – es wird \"Sehr geehrte Damen und Herren\" verwendet."
                : "Anrede: Am Kontakt ist keine Ansprechperson gepflegt – es wird \"Sehr geehrte Damen und Herren\" verwendet.");
            return FallbackSalutation;
        }

        return contact!.Salutation switch
        {
            MarketingSalutation.Herr => $"Sehr geehrter Herr {formalName}",
            MarketingSalutation.Frau => $"Sehr geehrte Frau {formalName}",
            // Geschlecht unbekannt: neutrale Form mit vollem Namen ist immer korrekt
            _ => $"Guten Tag {BuildDisplayName(contact)}"
        };
    }

    private static string ReplaceTokens(
        string text,
        Dictionary<string, string?> values,
        MarketingContact? contact,
        List<string> warnings)
        => MarketingTemplatePlaceholders.ReplaceTokens(text, token =>
        {
            if (!token.IsKnown)
            {
                AddOnce(warnings, $"Unbekannter Platzhalter {token.Raw} – bleibt unverändert im Text stehen.");
                return null;
            }

            var value = values[token.Normalized];
            if (string.IsNullOrEmpty(value))
            {
                AddOnce(warnings, contact is null
                    ? $"{token.Normalized}: Kein Kontakt verknüpft – der Platzhalter bleibt im Text stehen und muss vor dem Versand ersetzt werden."
                    : $"{token.Normalized}: Am Kontakt nicht gepflegt – der Platzhalter bleibt im Text stehen und muss vor dem Versand ersetzt werden.");
                return null;
            }

            return value;
        });

    private static void AddOnce(List<string> warnings, string message)
    {
        if (!warnings.Contains(message))
            warnings.Add(message);
    }

    private static string? Trimmed(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? JoinNonEmpty(params string?[] parts)
    {
        var joined = string.Join(' ', parts
            .Select(Trimmed)
            .Where(p => p is not null));
        return joined.Length == 0 ? null : joined;
    }
}
