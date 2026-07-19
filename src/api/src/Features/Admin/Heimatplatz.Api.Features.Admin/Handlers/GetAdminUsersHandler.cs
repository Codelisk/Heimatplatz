using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Admin.Handlers;

/// <summary>
/// Nutzerliste fuer den Intern-Bereich - neueste Registrierungen zuerst, mit
/// Inseratsanzahl pro Nutzer (correlated Subquery, Admin-Volumen ist klein).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin")]
public class GetAdminUsersHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<GetAdminUsersRequest, GetAdminUsersResponse>
{
    private const int MaxPageSize = 200;

    [MediatorHttpGet("/users", OperationId = "GetAdminUsers")]
    public async Task<GetAdminUsersResponse> Handle(GetAdminUsersRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var query = dbContext.Set<User>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(u =>
                (u.FirstName + " " + u.LastName).ToLower().Contains(search) ||
                u.Email.ToLower().Contains(search) ||
                (u.CompanyName ?? "").ToLower().Contains(search));
        }

        var page = Math.Max(request.Page, 0);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var total = await query.CountAsync(cancellationToken);
        var hasMore = (page + 1) * pageSize < total;

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .ThenBy(u => u.Id)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto(
                u.Id,
                u.FirstName + " " + u.LastName,
                u.Email,
                u.SellerType,
                u.CompanyName,
                u.IsAdmin,
                u.EmailVerifiedAt != null,
                u.CreatedAt,
                dbContext.Set<Property>().Count(p => p.UserId == u.Id)
            ))
            .ToListAsync(cancellationToken);

        return new GetAdminUsersResponse(users, total, pageSize, page, hasMore);
    }
}
