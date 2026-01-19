using System.IdentityModel.Tokens.Jwt;
using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Handler for RemoveBlockedRequest - removes a property from the user's blocked list (unblock)
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/blocked")]
public class RemoveBlockedHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<RemoveBlockedRequest, RemoveBlockedResponse>
{
    [MediatorHttpDelete("/{PropertyId}", OperationId = "RemoveBlocked", AuthorizationPolicies = [AuthorizationPolicies.RequireBuyer])]
    public async Task<RemoveBlockedResponse> Handle(RemoveBlockedRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        // Extract UserId from JWT Token
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext ist nicht verfuegbar");

        var userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Benutzer-ID nicht im Token gefunden");

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Ungueltige Benutzer-ID im Token");
        }

        // Find the blocked entry
        var blocked = await dbContext.Set<Blocked>()
            .FirstOrDefaultAsync(b => b.UserId == userId && b.PropertyId == request.PropertyId, cancellationToken);

        if (blocked == null)
        {
            return new RemoveBlockedResponse(false, "Blockierung nicht gefunden");
        }

        // Remove blocked entry
        dbContext.Set<Blocked>().Remove(blocked);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RemoveBlockedResponse(true, "Blockierung aufgehoben");
    }
}
