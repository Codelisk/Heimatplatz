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
/// Handler for DeletePropertyRequest - deletes a property owned by the authenticated user
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/properties")]
public class DeletePropertyHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<DeletePropertyRequest, DeletePropertyResponse>
{
    [MediatorHttpDelete("/{Id}", OperationId = "DeleteProperty", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireSeller])]
    public async Task<DeletePropertyResponse> Handle(DeletePropertyRequest request, IMediatorContext context, CancellationToken cancellationToken)
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

        // Load existing property
        var property = await dbContext.Set<Property>()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Property mit ID {request.Id} nicht gefunden");

        // Validate ownership
        if (property.UserId != userId)
        {
            throw new UnauthorizedAccessException("Sie haben keine Berechtigung, diese Immobilie zu loeschen");
        }

        // Delete property (hard delete)
        dbContext.Set<Property>().Remove(property);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeletePropertyResponse(
            Success: true,
            Message: "Immobilie erfolgreich geloescht"
        );
    }
}
