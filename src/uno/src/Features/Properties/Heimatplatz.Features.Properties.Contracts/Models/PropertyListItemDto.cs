namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>
/// DTO for property list view
/// </summary>
public record PropertyListItemDto(
    Guid Id,
    string Title,
    string Address,
    string City,
    decimal Price,
    int? LivingAreaM2,
    int? PlotAreaM2,
    int? Rooms,
    PropertyType Type,
    SellerType SellerType,
    string SellerName,
    List<string> ImageUrls,
    DateTime CreatedAt,
    InquiryType InquiryType
);
