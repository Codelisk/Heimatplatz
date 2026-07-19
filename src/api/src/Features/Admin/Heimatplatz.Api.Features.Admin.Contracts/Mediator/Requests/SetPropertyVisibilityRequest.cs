using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Admin.Contracts.Mediator.Requests;

/// <summary>
/// Blendet ein Inserat aus bzw. wieder ein (Admin-Moderation, Property.IsHidden).
/// Bewusst komplett im Body (kein Route-Parameter), damit das Binding des generierten
/// Endpoints eindeutig ist.
/// </summary>
public record SetPropertyVisibilityRequest(
    Guid Id,
    bool Hidden
) : IRequest<SetPropertyVisibilityResponse>;

public record SetPropertyVisibilityResponse(
    bool Success,
    bool IsHidden
);
