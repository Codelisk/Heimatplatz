using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Locations.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Abrufen der Location-Hierarchie (Bundesland -> Bezirk -> Gemeinde)
/// </summary>
public record GetLocationsRequest(
    string? FederalProvinceKey = null
) : IRequest<GetLocationsResponse>;

/// <summary>
/// Bundesland mit Bezirken und Gemeinden
/// </summary>
public record FederalProvinceDto(
    Guid Id,
    string Key,
    string Name,
    List<DistrictDto> Districts
);

/// <summary>
/// Bezirk mit Gemeinden
/// </summary>
public record DistrictDto(
    Guid Id,
    string Key,
    string Code,
    string Name,
    Guid FederalProvinceId,
    List<MunicipalityDto> Municipalities
);

/// <summary>
/// Gemeinde
/// </summary>
public record MunicipalityDto(
    Guid Id,
    string Key,
    string Code,
    string Name,
    string PostalCode,
    string? Status,
    Guid DistrictId
);

/// <summary>
/// Response mit der kompletten Location-Hierarchie
/// </summary>
public record GetLocationsResponse(
    List<FederalProvinceDto> FederalProvinces
);
