using System.Globalization;
using System.Text;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Services;

/// <summary>
/// Bewertet Bildanhaenge nach technischer Qualitaet und inhaltlicher Eignung.
/// Die Auswahl ist bewusst deterministisch, damit sich Content-Hashes nicht
/// allein durch einen erneuten Sync aendern.
/// </summary>
public static class ForeclosureImageSelector
{
    public const int MinimumPrimaryWidth = 640;
    public const int MinimumPrimaryHeight = 360;
    private const int MinimumFallbackWidth = 480;
    private const int MinimumFallbackHeight = 300;

    private static readonly string[] PlaceholderTerms =
    [
        "siehe beilagen", "siehe beilage", "kein foto", "kein bild", "symbolfoto", "platzhalter"
    ];

    private static readonly string[] NonPhotoTerms =
    [
        "lageplan", "grundriss", "flaechenwidmung", "widmungsplan", "kataster",
        "grundbuch", "schnitt", "ausfuehrungsplan"
    ];

    private static readonly (string Term, int Score)[] PositiveTerms =
    [
        ("wohnhaus", 500),
        ("aussenansicht", 450),
        ("strassenansicht", 400),
        ("gebaeude", 300),
        ("liegenschaft", 250),
        ("haus", 200),
        ("innenansicht", 150)
    ];

    private static readonly (string Term, int Score)[] NegativeTerms =
    [
        ("bootshuette", -500),
        ("garage", -120),
        ("stall", -80)
    ];

    /// <summary>
    /// Liefert direkt verlinkte Edikt-Fotos in Hero-Reihenfolge. Plaene,
    /// Platzhalter und technisch zu kleine Bilder werden nicht ausgegeben.
    /// </summary>
    public static List<EdiktImageCandidate> SelectDirectImages(
        IEnumerable<EdiktImageCandidate> candidates,
        bool requirePrimaryQuality = true,
        int maxCount = 20)
    {
        var minimumWidth = requirePrimaryQuality ? MinimumPrimaryWidth : MinimumFallbackWidth;
        var minimumHeight = requirePrimaryQuality ? MinimumPrimaryHeight : MinimumFallbackHeight;

        return candidates
            .Where(candidate => candidate.IsPhoto)
            .Where(candidate => !IsPlaceholderOrPlan(GetSearchText(candidate)))
            .Where(candidate => candidate.Width >= minimumWidth && candidate.Height >= minimumHeight)
            .OrderByDescending(ScoreDirectImage)
            .ThenBy(candidate => candidate.DocumentOrder)
            .Take(maxCount)
            .ToList();
    }

    internal static List<PdfImageCandidate> SelectPdfImages(
        IEnumerable<PdfImageCandidate> candidates,
        int maxCount)
    {
        return candidates
            .Where(candidate => candidate.PageNumber > 1)
            .Where(candidate => candidate.Width >= MinimumPrimaryWidth
                && candidate.Height >= MinimumPrimaryHeight)
            .Where(candidate => candidate.Bytes.Length >= 10 * 1024)
            .Where(candidate =>
            {
                var ratio = candidate.Width / (double)candidate.Height;
                return ratio is >= 0.70 and <= 2.40;
            })
            .Where(candidate => !IsPlaceholderOrPlan(Normalize(candidate.PageText)))
            .OrderByDescending(ScorePdfImage)
            .ThenBy(candidate => candidate.PageNumber)
            .ThenBy(candidate => candidate.ImageOrder)
            .Take(maxCount)
            .ToList();
    }

    private static int ScoreDirectImage(EdiktImageCandidate candidate)
    {
        var width = candidate.Width ?? 0;
        var height = candidate.Height ?? 0;
        var score = (int)Math.Min(((long)width * height) / 10_000, 1_000);
        var text = GetSearchText(candidate);

        score += ScoreTerms(text, PositiveTerms);
        score += ScoreTerms(text, NegativeTerms);
        score -= candidate.DocumentOrder;
        return score;
    }

    private static int ScorePdfImage(PdfImageCandidate candidate)
    {
        var score = (int)Math.Min(((long)candidate.Width * candidate.Height) / 10_000, 1_000);
        var text = Normalize(candidate.PageText);
        score += ScoreTerms(text, PositiveTerms);

        if (text.Contains("foto", StringComparison.Ordinal))
            score += 600;

        // Querformat ist fuer Karten und Hero-Galerien meist besser geeignet.
        if (candidate.Width >= candidate.Height)
            score += 150;

        return score;
    }

    private static int ScoreTerms(string text, IEnumerable<(string Term, int Score)> terms) =>
        terms.Where(term => text.Contains(term.Term, StringComparison.Ordinal))
            .Sum(term => term.Score);

    private static bool IsPlaceholderOrPlan(string text) =>
        PlaceholderTerms.Any(term => text.Contains(term, StringComparison.Ordinal))
        || NonPhotoTerms.Any(term => text.Contains(term, StringComparison.Ordinal));

    private static string GetSearchText(EdiktImageCandidate candidate)
    {
        string fileName;
        try
        {
            fileName = Uri.UnescapeDataString(new Uri(candidate.Url).Segments[^1]);
        }
        catch
        {
            fileName = candidate.Url;
        }

        return Normalize($"{fileName} {candidate.Title} {candidate.AltText}");
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

internal sealed record PdfImageCandidate(
    int PageNumber,
    int ImageOrder,
    string PageText,
    int Width,
    int Height,
    byte[] Bytes,
    string Extension);
