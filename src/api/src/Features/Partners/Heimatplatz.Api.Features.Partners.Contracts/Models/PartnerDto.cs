namespace Heimatplatz.Api.Features.Partners.Contracts.Models;

/// <summary>
/// Ein Partner fuer die oeffentliche /partner/-Seite und die Intern-Pflege.
/// ActiveListingCount wird zur Abfragezeit ueber Property.SourceName berechnet
/// (nur sichtbare Inserate), nie gespeichert.
/// </summary>
public record PartnerDto(
    Guid Id,
    string Name,
    string Category,
    string? Description,
    string? WebsiteUrl,
    string? LogoUrl,
    string? Region,
    int? PartnerSinceYear,
    string? SourceName,
    string? SellerName,
    int DisplayOrder,
    bool IsVisible,
    int ActiveListingCount
);
