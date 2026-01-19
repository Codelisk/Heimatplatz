using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to add a property to the user's blocked list.
/// Blocked properties are hidden from the main property list.
/// If the property is currently favorited, it will be removed from favorites.
/// </summary>
public record AddBlockedRequest(
    Guid PropertyId
) : IRequest<AddBlockedResponse>;

/// <summary>
/// Response after adding a blocked property
/// </summary>
public record AddBlockedResponse(
    bool Success,
    string? Message,
    bool WasRemovedFromFavorites = false
);
