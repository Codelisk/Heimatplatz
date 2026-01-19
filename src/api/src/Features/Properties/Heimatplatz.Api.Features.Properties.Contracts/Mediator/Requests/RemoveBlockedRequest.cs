using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to remove a property from the user's blocked list (unblock)
/// </summary>
public record RemoveBlockedRequest(
    Guid PropertyId
) : IRequest<RemoveBlockedResponse>;

/// <summary>
/// Response after removing a blocked property
/// </summary>
public record RemoveBlockedResponse(
    bool Success,
    string? Message
);
