using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to retrieve favorited properties for the authenticated user with pagination
/// </summary>
public record GetUserFavoritesRequest(
    int Page = 0,
    int PageSize = 20
) : IRequest<GetUserFavoritesResponse>;

/// <summary>
/// Response containing the user's favorited properties with pagination info
/// </summary>
public record GetUserFavoritesResponse(
    List<PropertyListItemDto> Properties,
    int Total,
    int PageSize,
    int CurrentPage,
    bool HasMore
);
