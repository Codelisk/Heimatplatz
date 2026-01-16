using Heimatplatz.Api.Features.Auth.Contracts.Enums;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Zuweisen einer Rolle an einen Benutzer
/// </summary>
public record AssignUserRoleRequest(
    Guid UserId,
    UserRoleType RoleType
) : IRequest<AssignUserRoleResponse>;

/// <summary>
/// Response nach erfolgreicher Rollenzuweisung
/// </summary>
public record AssignUserRoleResponse(
    Guid UserId,
    UserRoleType RoleType,
    bool WasAlreadyAssigned
);
