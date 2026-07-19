namespace Heimatplatz.Api.Features.Properties.Data.Seeding;

/// <summary>
/// Löst Gemeindenamen auf Municipality-Ids auf (Umlaut-normalisiert).
/// Gemeinsame Logik für PropertySeeder und PropertyMunicipalityFixSeeder.
/// </summary>
public static class MunicipalityNameResolver
{
    /// <summary>
    /// Normalisiert einen Gemeindenamen für den Vergleich: lowercase + Umlaut-Transliteration,
    /// damit "Voecklabruck" und "Vöcklabruck" denselben Schlüssel ergeben.
    /// </summary>
    public static string NormalizeCityKey(string value) => value
        .ToLowerInvariant()
        .Replace("ä", "ae")
        .Replace("ö", "oe")
        .Replace("ü", "ue")
        .Replace("ß", "ss")
        .Trim();

    /// <summary>
    /// Löst einen Gemeindenamen gegen das normalisierte Dictionary auf.
    /// Exakter Match zuerst, dann Präfix-Match (z.B. "Braunau" -> "Braunau am Inn").
    /// Liefert null statt einer falschen Zuordnung.
    /// </summary>
    public static Guid? Resolve(IReadOnlyDictionary<string, Guid> municipalitiesByKey, string cityName)
    {
        var key = NormalizeCityKey(cityName);
        if (municipalitiesByKey.TryGetValue(key, out var id))
            return id;

        // Präfix-Match nur in eine Richtung (gesuchter Name ist Kurzform des DB-Namens);
        // Contains in beide Richtungen würde z.B. "Linz" auf "Linz-Land"-Gemeinden mappen.
        var prefixMatches = municipalitiesByKey.Keys
            .Where(k => k.StartsWith(key + " ", StringComparison.Ordinal))
            .ToList();
        if (prefixMatches.Count == 1)
            return municipalitiesByKey[prefixMatches[0]];

        return null;
    }
}
