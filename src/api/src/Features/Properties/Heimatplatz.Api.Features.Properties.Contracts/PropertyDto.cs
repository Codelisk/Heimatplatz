using Heimatplatz.Api.Features.Properties.Contracts.Enums;

namespace Heimatplatz.Api.Features.Properties.Contracts;

/// <summary>
/// Full property data for detail view
/// </summary>
public record PropertyDto(
    Guid Id,
    string Title,
    string Address,
    Guid MunicipalityId,
    string City,
    string PostalCode,
    decimal Price,
    int? LivingAreaM2,
    int? PlotAreaM2,
    int? Rooms,
    int? YearBuilt,
    PropertyType Type,
    SellerType SellerType,
    string SellerName,
    string? Description,
    List<string> Features,
    List<string> ImageUrls,
    DateTimeOffset CreatedAt,
    InquiryType InquiryType,
    List<ContactInfoDto> Contacts,
    string? TypeSpecificData,
    // Serverseitig abgeleitete Anzeige-Fakten (Backend-First: Clients rendern nur)
    // "Kaufpreis" | "Mindestgebot" | "Schätzwert" - haengt bei Zwangsversteigerungen
    // davon ab, ob ein Mindestgebot vorliegt (Price = MinimumBid ?? EstimatedValue)
    string PriceLabel = "Kaufpreis",
    // Preis pro m² Wohnflaeche; null bei Zwangsversteigerungen oder fehlender Flaeche
    decimal? PricePerSquareMeter = null,
    // Skalierte Varianten derselben Bilder in identischer Reihenfolge (Backend-First:
    // Clients bauen keine Bild-URLs selbst zusammen). ImageUrls bleibt die volle
    // Display-Variante (Lightbox, Bearbeiten-Flow).
    // Preview: Detail-Ansicht/Carousel - deutlich kleiner als die bis zu 2560px breiten Originale.
    List<string>? PreviewImageUrls = null,
    // Thumbnail: byte-identisch mit den Listen-URLs, liegt auf Clients daher meist
    // schon im Bild-Cache und kann sofort als Platzhalter gezeigt werden.
    List<string>? ThumbnailImageUrls = null
);

/// <summary>
/// Compact property data for list view
/// </summary>
public record PropertyListItemDto(
    Guid Id,
    string Title,
    string Address,
    Guid MunicipalityId,
    string City,
    string PostalCode,
    decimal Price,
    int? LivingAreaM2,
    int? PlotAreaM2,
    int? Rooms,
    PropertyType Type,
    SellerType SellerType,
    string SellerName,
    List<string> ImageUrls,
    DateTimeOffset CreatedAt,
    InquiryType InquiryType,
    string? SourceName = null,
    DateTime? AuctionDate = null
);
