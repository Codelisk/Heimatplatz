using System.Text.Json;

namespace Heimatplatz.Api.Features.Marketing.Services;

/// <summary>
/// Parst die KI-Ausgabe der Entwurfs-Pruefung. Erwartet wird das im Prompt des
/// AiConnectorMarketingReplyChecker definierte JSON-Objekt
/// {"fitsContext": bool, "contextNote": "...", "correctedText": "..."|null,
/// "suggestedText": "..."|null} - typische Verpackungen (Markdown-Zaeune, Text
/// vor/nach dem JSON) werden tolerant entfernt (gleiches Muster wie
/// MarketingEmailOutputParser). Leere/Whitespace-Strings zaehlen als null.
/// </summary>
public static class MarketingReplyCheckOutputParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static MarketingReplyCheck Parse(string rawOutput)
    {
        var text = StripFences(rawOutput.Trim());

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new InvalidOperationException(
                $"KI-Antwort enthält kein JSON-Objekt mit dem Prüfergebnis: {Truncate(text, 200)}");

        CheckJson? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<CheckJson>(text[start..(end + 1)], JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"KI-Antwort ist kein gültiges JSON: {Truncate(text, 200)}", ex);
        }

        if (parsed?.FitsContext is null || string.IsNullOrWhiteSpace(parsed.ContextNote))
            throw new InvalidOperationException("KI-Antwort enthält keine Kontext-Einschätzung.");

        return new MarketingReplyCheck(
            parsed.FitsContext.Value,
            parsed.ContextNote.Trim(),
            NormalizeOptional(parsed.CorrectedText),
            NormalizeOptional(parsed.SuggestedText));
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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

    private sealed record CheckJson(
        bool? FitsContext,
        string? ContextNote,
        string? CorrectedText,
        string? SuggestedText);
}
