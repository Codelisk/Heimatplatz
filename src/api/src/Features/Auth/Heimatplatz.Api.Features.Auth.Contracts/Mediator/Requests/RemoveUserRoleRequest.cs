using Heimatplatz.Api.Features.Auth.Contracts.Enums;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Entfernen einer Rolle von einem Benutzer
/// </summary>
public record RemoveUserRoleRequest(
    Guid? UserId,
    UserRoleType? RoleType
) : IRequest<RemoveUserRoleResponse>;

/// <summary>
/// Response nach Entfernung einer Rolle
/// </summary>
public record RemoveUserRoleResponse(
    Guid UserId,
    UserRoleType RoleType,
    bool WasRemoved
);
