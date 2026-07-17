using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to retrieve all available property types (for type pickers in the clients)
/// </summary>
public record GetPropertyTypesRequest() : IRequest<GetPropertyTypesResponse>;

/// <summary>
/// DTO for a property type option: enum name as stable value plus German display label
/// </summary>
public record PropertyTypeOptionDto(
    string Value,
    string Label
);

/// <summary>
/// Response with the list of property types
/// </summary>
public record GetPropertyTypesResponse(
    List<PropertyTypeOptionDto> Types
);
