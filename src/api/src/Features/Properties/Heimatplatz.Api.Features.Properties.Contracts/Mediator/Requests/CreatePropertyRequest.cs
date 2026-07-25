using Heimatplatz.Api.Features.Properties.Contracts;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to create a new property.
/// SellerType/SellerName werden bewusst NICHT vom Client entgegengenommen -
/// der Server leitet sie aus dem Profil des authentifizierten Verkaeufers ab.
/// </summary>
public record CreatePropertyRequest(
    string Title,
    string Address,
    Guid MunicipalityId,
    decimal Price,
    PropertyType Type,
    string? Description = null,
    int? LivingAreaSquareMeters = null,
    int? PlotAreaSquareMeters = null,
    int? Rooms = null,
    int? YearBuilt = null,
    List<string>? Features = null,
    List<string>? ImageUrls = null,
    Dictionary<string, object>? TypeSpecificData = null,
    string? OriginalListingUrl = null,
    ContactPersonInput? ContactPerson = null,
    string? PostalCode = null,
    // Anzeige-Absicht fuer die Lage (Genau/Ungefaehr/Verborgen); null = Standard
    // Approximate (auch fuer aeltere Clients ohne das Feld)
    LocationDisplayMode? LocationDisplay = null
) : IRequest<CreatePropertyResponse>;

/// <summary>
/// Response after successful property creation
/// </summary>
public record CreatePropertyResponse(
    Guid PropertyId,
    string Title,
    DateTimeOffset CreatedAt
);
