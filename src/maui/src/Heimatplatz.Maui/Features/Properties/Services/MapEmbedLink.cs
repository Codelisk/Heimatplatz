using System.Globalization;
using System.Text;
using Heimatplatz.Maui.Features.Properties.Models;

namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Baut den Query-String fuer die Vollbild-Kartenansicht /karte-embed aus dem
/// aktuellen Filter-State. Die Params entsprechen 1:1 der Web-Startseite
/// (type, seller, region, age) - das Embed loest sie serverseitig mit dem
/// gleichen Query-Builder auf, Karte und App-Liste zeigen dieselben Treffer.
/// </summary>
public static class MapEmbedLink
{
    public static string BuildQuery(FilterState state, bool darkTheme)
    {
        var parts = new List<string> { "ansicht=karte", $"theme={(darkTheme ? "dark" : "light")}" };

        // Standard-Auswahl (Haus+Grund ohne Zwangsversteigerungen) entspricht dem
        // Web-Default und braucht keinen Param; alles andere explizit (auch die
        // Voll-Auswahl, sonst fiele das Embed auf den Default zurueck).
        var isDefaultTypes = state.IsHausSelected && state.IsGrundstueckSelected && !state.IsZwangsversteigerungSelected;
        if (!isDefaultTypes)
        {
            var types = new List<string>(3);
            if (state.IsHausSelected) types.Add("house");
            if (state.IsGrundstueckSelected) types.Add("land");
            if (state.IsZwangsversteigerungSelected) types.Add("foreclosure");
            if (types.Count > 0) parts.Add($"type={string.Join(",", types)}");
        }

        if (state.IsPrivateSelected != state.IsBrokerSelected)
            parts.Add($"seller={(state.IsPrivateSelected ? "private" : "agent")}");

        // Ortsnamen -> Web-Slugs, gleiche Normalisierung wie PropertySearchApp
        // beim Lesen der MAUI-Filter-Praeferenzen (normalize in search-query-Umfeld)
        var slugs = state.SelectedOrte
            .Select(Slugify)
            .Where(static slug => slug.Length > 0)
            .Distinct()
            .ToList();
        if (slugs.Count > 0) parts.Add($"region={Uri.EscapeDataString(string.Join(",", slugs))}");

        var age = state.SelectedAgeFilter switch
        {
            AgeFilter.EinTag => "day",
            AgeFilter.EineWoche => "week",
            AgeFilter.EinMonat => "month",
            AgeFilter.EinJahr => "year",
            _ => "",
        };
        if (age.Length > 0) parts.Add($"age={age}");

        return string.Join("&", parts);
    }

    /// <summary>
    /// Ortsname -> Slug, identisch zur Web-Normalisierung (Umlaut-Transliteration,
    /// Diakritika entfernen, Kleinbuchstaben, alles andere zu "-").
    /// </summary>
    internal static string Slugify(string value)
    {
        var transliterated = value
            .Replace("ä", "ae").Replace("Ä", "ae")
            .Replace("ö", "oe").Replace("Ö", "oe")
            .Replace("ü", "ue").Replace("Ü", "ue")
            .Replace("ß", "ss");

        // Nicht-alphanumerische LAEUFE zu genau einem "-" (wie [^a-z0-9]+ im Web)
        var builder = new StringBuilder(transliterated.Length);
        foreach (var ch in transliterated.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            var lower = char.ToLowerInvariant(ch);
            if (lower is >= 'a' and <= 'z' or >= '0' and <= '9')
                builder.Append(lower);
            else if (builder.Length > 0 && builder[^1] != '-')
                builder.Append('-');
        }

        return builder.ToString().Trim('-');
    }
}
