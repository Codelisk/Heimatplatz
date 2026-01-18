using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to retrieve all favorited properties for the authenticated user
/// </summary>
public record GetUserFavoritesRequest() : IRequest<GetUserFavoritesResponse>;

/// <summary>
/// Response containing the user's favorited properties
/// </summary>
public record GetUserFavoritesResponse(
    List<PropertyListItemDto> Properties
);
