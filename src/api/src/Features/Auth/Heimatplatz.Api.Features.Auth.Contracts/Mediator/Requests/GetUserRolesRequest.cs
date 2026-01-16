using Heimatplatz.Api.Features.Auth.Contracts.Enums;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Abrufen der Rollen eines Benutzers
/// </summary>
public record GetUserRolesRequest(
    Guid UserId
) : IRequest<GetUserRolesResponse>;

/// <summary>
/// Response mit den Rollen des Benutzers
/// </summary>
public record GetUserRolesResponse(
    Guid UserId,
    IReadOnlyList<UserRoleType> Roles,
    bool IsBuyer,
    bool IsSeller
);
