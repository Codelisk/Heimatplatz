using Heimatplatz.Api.Features.Partners.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Partners.Contracts.Models;

namespace Heimatplatz.Api.Features.Partners.Services;

/// <summary>
/// Fachliche Validierung fuer SavePartnerRequest. Statisch und ohne Abhaengigkeiten,
/// damit sie direkt unit-testbar ist (Muster ContactInfoFactory).
/// </summary>
public static class PartnerValidation
{
    public static string? Validate(SavePartnerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return "Der Name ist ein Pflichtfeld.";

        if (!PartnerCategories.All.Contains(request.Category))
            return $"Unbekannte Kategorie \"{request.Category}\". Erlaubt: {string.Join(", ", PartnerCategories.All)}.";

        if (InvalidHttpUrl(request.WebsiteUrl))
            return "Die Website muss mit http:// oder https:// beginnen.";

        if (InvalidLogoUrl(request.LogoUrl))
            return "Die Logo-URL muss vom Logo-Upload stammen (/uploads/...) oder mit http:// bzw. https:// beginnen.";

        if (request.PartnerSinceYear is < 1900 or > 2100)
            return "Das \"Partner seit\"-Jahr muss zwischen 1900 und 2100 liegen.";

        return null;
    }

    private static bool InvalidHttpUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps);
    }

    private static bool InvalidLogoUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Relative Upload-Pfade sind ok (werden von der API selbst ausgeliefert)
        if (value.Trim().StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return false;

        return InvalidHttpUrl(value);
    }
}
