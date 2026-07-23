using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Shiny;

namespace Heimatplatz.Api.Features.Marketing.Services;

/// <summary>
/// Platzhalter-Ersetzung fuer E-Mail-Vorlagen.
///
/// {anrede} ist bewusst die vollstaendige Anredefloskel ohne Satzzeichen, nicht nur der
/// Name: das Geschlecht ist im Firmenbuch nicht bekannt, deshalb faellt die Anrede ohne
/// Ansprechpartner auf "Sehr geehrte Damen und Herren" zurueck - mit Ansprechpartner auf
/// "Guten Tag {Name}". Beides ergibt zusammen mit dem Komma in der Vorlage einen
/// korrekten Satz und bleibt im Editor aenderbar.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class MarketingTemplateRenderer : IMarketingTemplateRenderer
{
    private const string FallbackSalutation = "Sehr geehrte Damen und Herren";

    public MarketingRenderedTemplate Render(MarketingEmailTemplate template, MarketingContact? contact)
    {
        var salutation = BuildSalutation(contact);
        var name = contact?.Name?.Trim() ?? string.Empty;
        var company = contact?.Company?.Trim() ?? string.Empty;
        var city = contact?.City?.Trim() ?? string.Empty;

        return new MarketingRenderedTemplate(
            Replace(template.Subject, salutation, name, company, city),
            Replace(template.Body, salutation, name, company, city));
    }

    private static string BuildSalutation(MarketingContact? contact)
    {
        var name = contact?.Name?.Trim();
        return string.IsNullOrWhiteSpace(name) ? FallbackSalutation : $"Guten Tag {name}";
    }

    private static string Replace(string text, string salutation, string name, string company, string city)
        => text
            .Replace(MarketingTemplatePlaceholders.Salutation, salutation, StringComparison.OrdinalIgnoreCase)
            .Replace(MarketingTemplatePlaceholders.Name, name, StringComparison.OrdinalIgnoreCase)
            .Replace(MarketingTemplatePlaceholders.Company, company, StringComparison.OrdinalIgnoreCase)
            .Replace(MarketingTemplatePlaceholders.City, city, StringComparison.OrdinalIgnoreCase);
}
