using System.IdentityModel.Tokens.Jwt;
using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Exceptions;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Laedt die vollstaendigen Edit-Daten erst nach serverseitiger Eigentumspruefung.
/// Der oeffentliche Detail-Endpunkt bleibt davon unberuehrt.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/properties")]
public class GetEditablePropertyHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IMediator mediator
) : IRequestHandler<GetEditablePropertyRequest, GetPropertyByIdResponse>
{
    [MediatorHttpGet("/{Id}/edit", OperationId = "GetEditableProperty", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireSeller])]
    public async Task<GetPropertyByIdResponse> Handle(
        GetEditablePropertyRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext ist nicht verfuegbar");

        var userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Benutzer-ID nicht im Token gefunden");

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Ungueltige Benutzer-ID im Token");
        }

        var owner = await dbContext.Set<Property>()
            .Where(property => property.Id == request.Id && !property.IsHidden)
            .Select(property => new { property.UserId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Property mit ID {request.Id} nicht gefunden");

        if (owner.UserId != userId)
        {
            throw new ForbiddenException("Sie haben keine Berechtigung, diese Immobilie zu bearbeiten");
        }

        var mediatorResult = await mediator.Request(
            new GetPropertyByIdRequest(request.Id),
            cancellationToken);
        var response = mediatorResult.Result;

        return response.Property != null
            ? response
            : throw new NotFoundException($"Property mit ID {request.Id} nicht gefunden");
    }
}
