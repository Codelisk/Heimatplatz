using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to retrieve blocked properties for the authenticated user with pagination
/// </summary>
public record GetUserBlockedRequest(
    int Page = 0,
    int PageSize = 20
) : IRequest<GetUserBlockedResponse>;

/// <summary>
/// Response containing the user's blocked properties with pagination info
/// </summary>
public record GetUserBlockedResponse(
    List<PropertyListItemDto> Properties,
    int Total,
    int PageSize,
    int CurrentPage,
    bool HasMore
);
