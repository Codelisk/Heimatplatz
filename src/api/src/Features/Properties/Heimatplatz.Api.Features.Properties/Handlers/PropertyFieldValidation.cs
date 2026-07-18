namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Gemeinsame Validierung der Kernfelder fuer Create- und UpdatePropertyHandler.
/// Titel/Preis/Beschreibung/Adresse pruefen die Handler bereits selbst - hier
/// liegen die optionalen Zahlenfelder und die Merkmalliste (Backend-First:
/// die Clients liefern nur UX-Hinweise, verlaesslich validiert wird hier).
/// </summary>
internal static class PropertyFieldValidation
{
    private const int MaxLivingAreaSquareMeters = 10_000;
    private const int MaxPlotAreaSquareMeters = 1_000_000;
    private const int MaxRooms = 200;
    private const int MinYearBuilt = 1000;
    private const int MaxFeatureCount = 50;
    private const int MaxFeatureLength = 100;

    // Spiegel der EF-Spaltenlaenge (PropertyContactInfoConfiguration)
    private const int MaxOriginalListingUrlLength = 2000;

    public static void ValidateCoreFields(int? livingAreaSquareMeters, int? plotAreaSquareMeters, int? rooms, int? yearBuilt)
    {
        if (livingAreaSquareMeters is <= 0 or > MaxLivingAreaSquareMeters)
        {
            throw new ArgumentException($"Living area must be between 1 and {MaxLivingAreaSquareMeters} m²", nameof(livingAreaSquareMeters));
        }

        if (plotAreaSquareMeters is <= 0 or > MaxPlotAreaSquareMeters)
        {
            throw new ArgumentException($"Plot area must be between 1 and {MaxPlotAreaSquareMeters} m²", nameof(plotAreaSquareMeters));
        }

        if (rooms is <= 0 or > MaxRooms)
        {
            throw new ArgumentException($"Rooms must be between 1 and {MaxRooms}", nameof(rooms));
        }

        // Baujahr darf nicht in der Zukunft liegen (Kundenanforderung 17.7.2026)
        var currentYear = DateTime.UtcNow.Year;
        if (yearBuilt.HasValue && (yearBuilt < MinYearBuilt || yearBuilt > currentYear))
        {
            throw new ArgumentException($"Year built must be between {MinYearBuilt} and {currentYear}", nameof(yearBuilt));
        }
    }

    /// <summary>
    /// Merkmale normalisieren: trimmen, Leere und Duplikate entfernen, Limits pruefen.
    /// </summary>
    public static List<string> NormalizeFeatures(List<string>? features)
    {
        var normalized = (features ?? [])
            .Select(feature => feature?.Trim() ?? string.Empty)
            .Where(feature => feature.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count > MaxFeatureCount)
        {
            throw new ArgumentException($"At most {MaxFeatureCount} features are allowed", nameof(features));
        }

        if (normalized.Any(feature => feature.Length > MaxFeatureLength))
        {
            throw new ArgumentException($"Each feature must be at most {MaxFeatureLength} characters", nameof(features));
        }

        return normalized;
    }

    /// <summary>
    /// Optionale Original-Inserats-URL normalisieren: leer wird null,
    /// alles andere muss eine absolute http(s)-URL innerhalb der Spaltenlaenge sein.
    /// </summary>
    public static string? NormalizeOriginalListingUrl(string? originalListingUrl)
    {
        var trimmed = originalListingUrl?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (trimmed.Length > MaxOriginalListingUrlLength
            || !Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                $"Original listing URL must be an absolute http(s) URL with at most {MaxOriginalListingUrlLength} characters",
                nameof(originalListingUrl));
        }

        return trimmed;
    }
}
