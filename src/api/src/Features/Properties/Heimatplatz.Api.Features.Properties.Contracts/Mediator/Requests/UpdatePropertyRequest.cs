using Heimatplatz.Api.Features.Properties.Contracts;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to update an existing property.
/// SellerType/SellerName werden bewusst NICHT vom Client entgegengenommen -
/// der Server leitet sie aus dem Profil des Eigentuemers ab.
/// Note: Using class with properties (not record) for Shiny Mediator OpenAPI generator compatibility
/// </summary>
public class UpdatePropertyRequest : IRequest<UpdatePropertyResponse>
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public Guid MunicipalityId { get; set; }
    public decimal Price { get; set; }
    public PropertyType Type { get; set; }
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
