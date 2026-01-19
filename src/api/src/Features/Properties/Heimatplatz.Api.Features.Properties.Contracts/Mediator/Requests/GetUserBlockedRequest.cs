using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to retrieve all blocked properties for the authenticated user
/// </summary>
public record GetUserBlockedRequest() : IRequest<GetUserBlockedResponse>;

/// <summary>
/// Response containing the user's blocked properties
/// </summary>
public record GetUserBlockedResponse(
    List<PropertyListItemDto> Properties
);
