using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Handlers;

/// <summary>
/// Handler zum Entfernen einer Rolle von einem Benutzer
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class RemoveUserRoleHandler(
    AppDbContext dbContext
) : IRequestHandler<RemoveUserRoleRequest, RemoveUserRoleResponse>
{
    [MediatorHttpDelete("/api/users/roles", OperationId = "RemoveUserRole")]
    public async Task<RemoveUserRoleResponse> Handle(RemoveUserRoleRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        // Validierung der Pflichtfelder
        if (!request.UserId.HasValue)
            throw new ArgumentException("UserId ist erforderlich.");
        if (!request.RoleType.HasValue)
            throw new ArgumentException("RoleType ist erforderlich.");

        var userId = request.UserId.Value;
        var roleType = request.RoleType.Value;

        // Rolle suchen
        var existingRole = await dbContext.Set<UserRole>()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.RoleType == roleType, cancellationToken);

        if (existingRole == null)
        {
            return new RemoveUserRoleResponse(userId, roleType, WasRemoved: false);
        }

        // Rolle entfernen
        dbContext.Set<UserRole>().Remove(existingRole);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RemoveUserRoleResponse(userId, roleType, WasRemoved: true);
    }
}
