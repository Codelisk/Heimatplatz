namespace Heimatplatz.Api.Features.Properties.Contracts.Dtos;

/// <summary>
/// DTO for UpdateProperty request body (Id comes from route parameter)
/// </summary>
public class UpdatePropertyRequestDto
{
    public required string Title { get; set; }
    public required string Address { get; set; }
    public required string City { get; set; }
    public required string PostalCode { get; set; }
    public required decimal Price { get; set; }
    public required PropertyType Type { get; set; }
    public required SellerType SellerType { get; set; }
    public required string SellerName { get; set; }
    public string? Description { get; set; }
    public int? LivingAreaSquareMeters { get; set; }
    public int? PlotAreaSquareMeters { get; set; }
    public int? Rooms { get; set; }
    public int? YearBuilt { get; set; }
    public List<string>? Features { get; set; }
    public List<string>? ImageUrls { get; set; }
    public Dictionary<string, object>? TypeSpecificData { get; set; }
}

/// <summary>
/// DTO for UpdateProperty response
/// </summary>
public record UpdatePropertyResponseDto(
    Guid Id,
    string Title,
    DateTimeOffset? UpdatedAt
);
