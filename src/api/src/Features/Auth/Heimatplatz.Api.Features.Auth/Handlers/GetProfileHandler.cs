using System.IdentityModel.Tokens.Jwt;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Handlers;

/// <summary>
/// Handler zum Abrufen des eigenen Profils (GET /api/auth/profile)
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class GetProfileHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GetProfileRequest, GetProfileResponse>
{
    [MediatorHttpGet("/api/auth/profile", OperationId = "GetProfile", RequiresAuthorization = true)]
    public async Task<GetProfileResponse> Handle(GetProfileRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();

        var user = await dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Benutzer nicht gefunden.");

        return new GetProfileResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.FullName,
            user.SellerType,
            user.CompanyName,
            user.IsAdmin,
            user.EmailVerifiedAt is not null
        );
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Benutzer-ID nicht gefunden.");

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Ungueltige Benutzer-ID.");
        }

        return userId;
    }
}
