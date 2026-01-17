using Heimatplatz.Api.Features.Properties.Contracts;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to create a new property
/// </summary>
public record CreatePropertyRequest(
    string Title,
    string Address,
    string City,
    string PostalCode,
    decimal Price,
    PropertyType Type,
    SellerType SellerType,
    string SellerName,
    string? Description = null,
    int? LivingAreaSquareMeters = null,
    int? PlotAreaSquareMeters = null,
    int? Rooms = null,
    int? YearBuilt = null,
    List<string>? Features = null,
    List<string>? ImageUrls = null,
    Dictionary<string, object>? TypeSpecificData = null
) : IRequest<CreatePropertyResponse>;

/// <summary>
/// Response after successful property creation
/// </summary>
public record CreatePropertyResponse(
    Guid PropertyId,
    string Title,
    DateTimeOffset CreatedAt
);
