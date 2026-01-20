using Heimatplatz.Api.Features.Properties.Contracts.Enums;

namespace Heimatplatz.Api.Features.Properties.Contracts;

/// <summary>
/// Full property data for detail view
/// </summary>
public record PropertyDto(
    Guid Id,
    string Title,
    string Address,
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
    List<ContactInfoDto> Contacts
);

/// <summary>
/// Compact property data for list view
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
    DateTimeOffset CreatedAt,
    InquiryType InquiryType
);
