using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Contracts.Enums;
using Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Handlers;

/// <summary>
/// Handler zum Abrufen der Rollen eines Benutzers
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class GetUserRolesHandler(
    AppDbContext dbContext
) : IRequestHandler<GetUserRolesRequest, GetUserRolesResponse>
{
    [MediatorHttpGet("/api/users/{UserId}/roles", OperationId = "GetUserRoles")]
    public async Task<GetUserRolesResponse> Handle(GetUserRolesRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var roles = await dbContext.Set<UserRole>()
            .Where(r => r.UserId == request.UserId)
            .Select(r => r.RoleType)
            .ToListAsync(cancellationToken);

        return new GetUserRolesResponse(
            request.UserId,
            roles,
            IsBuyer: roles.Contains(UserRoleType.Buyer),
            IsSeller: roles.Contains(UserRoleType.Seller)
        );
    }
}
