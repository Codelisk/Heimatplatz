using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Handlers;

/// <summary>
/// Handler zum Zuweisen einer Rolle an einen Benutzer
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class AssignUserRoleHandler(
    AppDbContext dbContext
) : IRequestHandler<AssignUserRoleRequest, AssignUserRoleResponse>
{
    [MediatorHttpPost("/api/users/roles", OperationId = "AssignUserRole")]
    public async Task<AssignUserRoleResponse> Handle(AssignUserRoleRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        // Pruefen ob User existiert
        var userExists = await dbContext.Set<User>()
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);

        if (!userExists)
        {
            throw new InvalidOperationException($"Benutzer mit ID {request.UserId} nicht gefunden.");
        }

        // Pruefen ob Rolle bereits zugewiesen ist
        var existingRole = await dbContext.Set<UserRole>()
            .FirstOrDefaultAsync(r => r.UserId == request.UserId && r.RoleType == request.RoleType, cancellationToken);

        if (existingRole != null)
        {
            return new AssignUserRoleResponse(request.UserId, request.RoleType, WasAlreadyAssigned: true);
        }

        // Neue Rolle erstellen
        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            RoleType = request.RoleType,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Set<UserRole>().Add(userRole);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AssignUserRoleResponse(request.UserId, request.RoleType, WasAlreadyAssigned: false);
    }
}
