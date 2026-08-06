using System.Text.Json;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Heimatplatz.Api.Features.Dashboards.Infrastructure;

namespace Heimatplatz.Api.Features.Dashboards.Services;

/// <summary>
/// Parst die KI-Ausgabe des Dashboard-Designers. Erwartet wird das im Prompt
/// definierte DashboardDefinition-JSON; typische Verpackungen (Markdown-Zaeune,
/// Text vor/nach dem JSON) werden tolerant entfernt (gleiches Muster wie
/// MarketingEmailOutputParser). Die fachliche Pruefung uebernimmt danach der
/// DashboardDefinitionValidator - hier geht es nur um "ist es unser JSON".
/// </summary>
public static class DashboardOutputParser
{
    public static DashboardDefinition Parse(string rawOutput)
    {
        var text = StripFences(rawOutput.Trim());

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new InvalidOperationException(
                $"KI-Antwort enthaelt kein JSON-Objekt mit der Dashboard-Definition: {Truncate(text, 200)}");

        DashboardDefinition? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<DashboardDefinition>(
                text[start..(end + 1)], DashboardDefinitionSerializer.JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"KI-Antwort ist kein gueltiges JSON: {Truncate(text, 200)}", ex);
        }

        if (parsed is null)
            throw new InvalidOperationException("KI-Antwort enthaelt keine Dashboard-Definition.");

        return parsed;
    }

    private static string StripFences(string text)
    {
        if (!text.StartsWith("```", StringComparison.Ordinal))
            return text;

        var firstLineEnd = text.IndexOf('\n');
        var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
        return firstLineEnd >= 0 && lastFence > firstLineEnd
            ? text[(firstLineEnd + 1)..lastFence].Trim()
            : text;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";
}
