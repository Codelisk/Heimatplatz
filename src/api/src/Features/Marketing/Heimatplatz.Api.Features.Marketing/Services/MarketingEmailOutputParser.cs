using System.Text.Json;

namespace Heimatplatz.Api.Features.Marketing.Services;

/// <summary>
/// Parst die KI-Ausgabe der E-Mail-Generierung. Erwartet wird das in
/// sections/marketing/email/AGENTS.md definierte JSON-Objekt
/// {"subject": "...", "body": "..."} - typische Verpackungen (Markdown-Zaeune,
/// Text vor/nach dem JSON) werden tolerant entfernt.
/// </summary>
public static class MarketingEmailOutputParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static MarketingEmailDraft Parse(string rawOutput)
    {
        var text = StripFences(rawOutput.Trim());

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new InvalidOperationException(
                $"KI-Antwort enthält kein JSON-Objekt mit Betreff und Text: {Truncate(text, 200)}");

        DraftJson? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<DraftJson>(text[start..(end + 1)], JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"KI-Antwort ist kein gültiges JSON: {Truncate(text, 200)}", ex);
        }

        if (parsed is null || string.IsNullOrWhiteSpace(parsed.Subject) || string.IsNullOrWhiteSpace(parsed.Body))
            throw new InvalidOperationException("KI-Antwort enthält keinen Betreff oder keinen Text.");

        return new MarketingEmailDraft(parsed.Subject.Trim(), parsed.Body.Trim());
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

    private sealed record DraftJson(string? Subject, string? Body);
}
