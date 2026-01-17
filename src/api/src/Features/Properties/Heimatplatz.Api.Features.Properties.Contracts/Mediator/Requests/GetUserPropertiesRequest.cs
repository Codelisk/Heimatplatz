using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to retrieve all properties belonging to a specific user
/// </summary>
public record GetUserPropertiesRequest() : IRequest<GetUserPropertiesResponse>;

/// <summary>
/// Response containing the user's properties
/// </summary>
public record GetUserPropertiesResponse(
    List<PropertyListItemDto> Properties
);
