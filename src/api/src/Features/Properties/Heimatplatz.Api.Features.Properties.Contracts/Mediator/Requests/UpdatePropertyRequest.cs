using Heimatplatz.Api.Features.Properties.Contracts;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to update an existing property
/// Note: Using class with properties (not record) for Shiny Mediator OpenAPI generator compatibility
/// </summary>
public class UpdatePropertyRequest : IRequest<UpdatePropertyResponse>
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public PropertyType Type { get; set; }
    public SellerType SellerType { get; set; }
    public string SellerName { get; set; } = string.Empty;
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
/// Response after successful property update
/// </summary>
public record UpdatePropertyResponse(
    Guid Id,
    string Title,
    DateTimeOffset? UpdatedAt
);
