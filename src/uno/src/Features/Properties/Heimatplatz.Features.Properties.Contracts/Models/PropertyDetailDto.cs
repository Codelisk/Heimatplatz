namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>
/// DTO for property detail view with extended information
/// </summary>
public record PropertyDetailDto(
    Guid Id,
    string Title,
    string Address,
    string City,
    decimal Price,
    int? LivingAreaM2,
    int? PlotAreaM2,
    int? Rooms,
    int? YearBuilt,
    PropertyType Type,
    SellerType SellerType,
    string SellerName,
    List<string> ImageUrls,
    string? Description,
    List<string> Features,
    InquiryType InquiryType,
    List<ContactInfoDto> Contacts
);
