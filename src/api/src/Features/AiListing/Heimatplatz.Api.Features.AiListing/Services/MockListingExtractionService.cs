using System.Globalization;
using System.Text.RegularExpressions;
using Heimatplatz.Api.Features.AiListing.Contracts.Models;
using Heimatplatz.Api.Features.Properties.Contracts;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.AiListing.Services;

/// <summary>
/// Dev-Provider ohne echte KI: extrahiert Kennzahlen heuristisch aus dem Diktat
/// (Zimmer, Flaechen, Baujahr, Ausstattung) und generiert Titel/Beschreibung.
/// Ermoeglicht das End-to-End-Testen des Flows ohne Agent-CLI am Server.
/// </summary>
public partial class MockListingExtractionService(
    ILogger<MockListingExtractionService> logger
) : IListingExtractionService
{
    private static readonly string[] KnownFeatures =
    [
        "Garage", "Carport", "Garten", "Keller", "Balkon", "Terrasse", "Pool",
        "Sauna", "Photovoltaik", "Waermepumpe", "Kamin", "Wintergarten",
        "Fussbodenheizung", "Solaranlage", "Doppelverglasung", "Smart Home"
    ];

    public async Task<ExtractedListingData> ExtractAsync(ListingExtractionInput input, CancellationToken ct = default)
    {
        // Simulierte Analysedauer, damit der Status-Flow in der App sichtbar ist
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        var text = $"{input.DictatedText} {input.UserNotes}".Trim();
        var normalized = text.ToLowerInvariant();

        var rooms = MatchInt(RoomsRegex(), normalized);
        var livingArea = MatchInt(LivingAreaRegex(), normalized)
            ?? MatchInt(LivingAreaBeforeKeywordRegex(), normalized);
        var plotArea = MatchInt(PlotAreaRegex(), normalized);
        var yearBuilt = MatchInt(YearBuiltRegex(), normalized);

        // Flaeche ohne Kontext: erster m²-Wert als Wohnflaeche interpretieren
        if (livingArea is null && plotArea is null)
        {
            var anyArea = MatchInt(AnyAreaRegex(), normalized);
            livingArea = anyArea;
        }

        var isLand = normalized.Contains("grundstück") || normalized.Contains("grundstueck") || normalized.Contains("baugrund");
        var isHouse = normalized.Contains("haus") || normalized.Contains("wohnung") || rooms is not null || livingArea is not null;
        var type = isLand && !isHouse ? PropertyType.Land : PropertyType.House;

        var features = KnownFeatures
            .Where(f => normalized.Contains(f.ToLowerInvariant()))
            .ToList();

        var title = BuildTitle(type, rooms, livingArea, plotArea);
        var description = BuildDescription(text, type, rooms, livingArea, plotArea, yearBuilt, features);

        logger.LogInformation(
            "[AiListing] Mock-Extraktion abgeschlossen: {Title} (Zimmer={Rooms}, Wfl={LivingArea}, Gfl={PlotArea})",
            title, rooms, livingArea, plotArea);

        return new ExtractedListingData(
            Title: title,
            Description: description,
            Type: type,
            Rooms: type == PropertyType.House ? rooms : null,
            LivingAreaSquareMeters: type == PropertyType.House ? livingArea : null,
            PlotAreaSquareMeters: plotArea,
            YearBuilt: yearBuilt,
            Features: features,
            Summary: $"Mock-Analyse aus {input.ImagePaths.Count} Foto(s), {input.VideoPaths.Count} Video(s) und dem Diktat. " +
                     "Am Server erfolgt die Analyse durch die Agent-CLI."
        );
    }

    private static string BuildTitle(PropertyType type, int? rooms, int? livingArea, int? plotArea)
    {
        if (type == PropertyType.Land)
        {
            return plotArea is not null
                ? $"Sonniges Grundstück mit {plotArea} m² in guter Lage"
                : "Attraktives Grundstück in guter Lage";
        }

        var parts = new List<string>();
        if (rooms is not null) parts.Add($"{rooms} Zimmer");
        if (livingArea is not null) parts.Add($"{livingArea} m² Wohnfläche");

        return parts.Count > 0
            ? $"Charmantes Einfamilienhaus – {string.Join(", ", parts)}"
            : "Charmantes Einfamilienhaus in guter Lage";
    }

    private static string BuildDescription(
        string dictatedText, PropertyType type, int? rooms, int? livingArea,
        int? plotArea, int? yearBuilt, List<string> features)
    {
        var facts = new List<string>();
        if (rooms is not null) facts.Add($"{rooms} Zimmer");
        if (livingArea is not null) facts.Add($"ca. {livingArea} m² Wohnfläche");
        if (plotArea is not null) facts.Add($"ca. {plotArea} m² Grundfläche");
        if (yearBuilt is not null) facts.Add($"Baujahr {yearBuilt}");

        var objectWord = type == PropertyType.Land ? "Grundstück" : "Objekt";
        var intro = facts.Count > 0
            ? $"Dieses {objectWord} überzeugt mit {string.Join(", ", facts)}."
            : $"Dieses {objectWord} befindet sich in einer attraktiven Lage.";

        var featureSentence = features.Count > 0
            ? $" Zur Ausstattung zählen: {string.Join(", ", features)}."
            : string.Empty;

        var dictated = string.IsNullOrWhiteSpace(dictatedText)
            ? string.Empty
            : $"\n\n{dictatedText.Trim()}";

        return $"{intro}{featureSentence}{dictated}";
    }

    private static int? MatchInt(Regex regex, string text)
    {
        var match = regex.Match(text);
        return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    [GeneratedRegex(@"(\d+)\s*(?:zimmer|schlafzimmer)")]
    private static partial Regex RoomsRegex();

    [GeneratedRegex(@"(?:wohnfläche|wohnflaeche|wohnbereich)\D{0,10}(\d+)")]
    private static partial Regex LivingAreaRegex();

    [GeneratedRegex(@"(\d+)\s*(?:m2|m²|qm|quadratmeter)\s*(?:wohnfläche|wohnflaeche)")]
    private static partial Regex LivingAreaBeforeKeywordRegex();

    [GeneratedRegex(@"(?:grundstück|grundstueck|grundfläche|grundflaeche|baugrund)\D{0,20}(\d+)")]
    private static partial Regex PlotAreaRegex();

    [GeneratedRegex(@"(?:baujahr|gebaut)\D{0,10}(1[89]\d{2}|20\d{2})")]
    private static partial Regex YearBuiltRegex();

    [GeneratedRegex(@"(\d+)\s*(?:m2|m²|quadratmeter)")]
    private static partial Regex AnyAreaRegex();
}
