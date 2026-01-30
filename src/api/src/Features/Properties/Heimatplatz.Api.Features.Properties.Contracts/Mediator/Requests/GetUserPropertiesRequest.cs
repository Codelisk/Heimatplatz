using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to retrieve properties belonging to the authenticated user with pagination
/// </summary>
public record GetUserPropertiesRequest(
    int Page = 0,
    int PageSize = 20
) : IRequest<GetUserPropertiesResponse>;

/// <summary>
/// Response containing the user's properties with pagination info
/// </summary>
public record GetUserPropertiesResponse(
    List<PropertyListItemDto> Properties,
    int Total,
    int PageSize,
    int CurrentPage,
    bool HasMore
);
