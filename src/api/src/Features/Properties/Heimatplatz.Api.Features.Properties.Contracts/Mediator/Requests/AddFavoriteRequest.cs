using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to add a property to the user's favorites
/// </summary>
public record AddFavoriteRequest(
    Guid PropertyId
) : IRequest<AddFavoriteResponse>;

/// <summary>
/// Response after adding a favorite
/// </summary>
public record AddFavoriteResponse(
    bool Success,
    string? Message
);
