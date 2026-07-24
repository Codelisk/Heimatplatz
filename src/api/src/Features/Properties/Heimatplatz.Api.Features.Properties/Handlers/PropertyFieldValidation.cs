using Heimatplatz.Api.Features.Auth.Services;
using Heimatplatz.Api.Features.Properties.Contracts;

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

    // Spiegel der EF-Spaltenlaengen (PropertyContactInfoConfiguration)
    private const int MaxOriginalListingUrlLength = 2000;
    private const int MaxContactNameLength = 200;
    private const int MaxContactEmailLength = 254;

    // Spiegel der EF-Spaltenlaenge (PropertyConfiguration.PostalCode)
    private const int MaxPostalCodeLength = 10;

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
    /// Vom Nutzer eingegebene PLZ normalisieren (WEB-009): leer wird null (dann gilt
    /// Municipality.PostalCode), sonst getrimmt mit Laengen-/Zeichenpruefung.
    /// </summary>
    public static string? NormalizePostalCode(string? postalCode)
    {
        var trimmed = postalCode?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (trimmed.Length > MaxPostalCodeLength || !trimmed.All(char.IsAsciiDigit))
        {
            throw new ArgumentException(
                $"Postal code must be numeric with at most {MaxPostalCodeLength} digits",
                nameof(postalCode));
        }

        return trimmed;
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

    /// <summary>
    /// Optionalen Ansprechpartner normalisieren: komplett leere Eingaben gelten als
    /// "kein Ansprechpartner" (null). Sonst ist der Name Pflicht und mindestens eine
    /// Erreichbarkeit (E-Mail oder Telefon) erforderlich - Kontakte ohne beides
    /// wuerden in den Detailansichten ohnehin nicht angezeigt.
    /// </summary>
    public static ContactPersonInput? NormalizeContactPerson(ContactPersonInput? contactPerson)
    {
        if (contactPerson == null)
        {
            return null;
        }

        var name = contactPerson.Name?.Trim();
        var hasEmail = !string.IsNullOrWhiteSpace(contactPerson.Email);
        var hasPhone = !string.IsNullOrWhiteSpace(contactPerson.Phone);

        if (string.IsNullOrEmpty(name) && !hasEmail && !hasPhone)
        {
            return null;
        }

        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Contact person name is required", nameof(contactPerson));
        }

        if (name.Length > MaxContactNameLength)
        {
            throw new ArgumentException($"Contact person name must be at most {MaxContactNameLength} characters", nameof(contactPerson));
        }

        var email = hasEmail ? UserInputValidator.NormalizeAndValidateEmail(contactPerson.Email) : null;
        if (email is { Length: > MaxContactEmailLength })
        {
            throw new ArgumentException($"Contact person email must be at most {MaxContactEmailLength} characters", nameof(contactPerson));
        }

        var phone = UserInputValidator.NormalizePhone(contactPerson.Phone);

        if (email == null && phone == null)
        {
            throw new ArgumentException("Contact person needs an email address or a phone number", nameof(contactPerson));
        }

        return new ContactPersonInput { Name = name, Email = email, Phone = phone };
    }
}
