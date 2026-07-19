using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Admin.Handlers;

/// <summary>
/// Kennzahlen fuer das Intern-Dashboard: Nutzerwachstum und Inseratsbestand.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin")]
public class GetAdminStatsHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<GetAdminStatsRequest, GetAdminStatsResponse>
{
    [MediatorHttpGet("/stats", OperationId = "GetAdminStats")]
    public async Task<GetAdminStatsResponse> Handle(GetAdminStatsRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var now = DateTimeOffset.UtcNow;
        var since7Days = now.AddDays(-7);
        var since30Days = now.AddDays(-30);

        var users = dbContext.Set<User>();
        var properties = dbContext.Set<Property>();

        return new GetAdminStatsResponse(
            TotalUsers: await users.CountAsync(cancellationToken),
            NewUsers7Days: await users.CountAsync(u => u.CreatedAt >= since7Days, cancellationToken),
            NewUsers30Days: await users.CountAsync(u => u.CreatedAt >= since30Days, cancellationToken),
            TotalProperties: await properties.CountAsync(cancellationToken),
            UserProperties: await properties.CountAsync(p => p.Type != PropertyType.Foreclosure, cancellationToken),
            ForeclosureProperties: await properties.CountAsync(p => p.Type == PropertyType.Foreclosure, cancellationToken),
            HiddenProperties: await properties.CountAsync(p => p.IsHidden, cancellationToken)
        );
    }
}
