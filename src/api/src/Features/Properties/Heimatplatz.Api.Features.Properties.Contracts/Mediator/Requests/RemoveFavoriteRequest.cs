using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to remove a property from the user's favorites
/// </summary>
public record RemoveFavoriteRequest(
    Guid PropertyId
) : IRequest<RemoveFavoriteResponse>;

/// <summary>
/// Response after removing a favorite
/// </summary>
public record RemoveFavoriteResponse(
    bool Success,
    string? Message
);
