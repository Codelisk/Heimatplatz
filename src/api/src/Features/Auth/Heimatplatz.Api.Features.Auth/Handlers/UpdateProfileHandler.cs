using System.IdentityModel.Tokens.Jwt;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Handlers;

/// <summary>
/// Handler zum Aktualisieren des eigenen Profils (PUT /api/auth/profile).
/// Ein Benutzer kann damit auch nachtraeglich Verkaeufer werden, den Anbietertyp
/// wechseln oder die Verkaeufer-Eigenschaft ablegen - die Registrierungsentscheidung
/// ist nicht mehr endgueltig. Bestehende Inserate behalten ihre historischen
/// Anbieter-Daten; neue/aktualisierte Inserate nutzen das aktuelle Profil.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class UpdateProfileHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    ITokenService tokenService
) : IRequestHandler<UpdateProfileRequest, UpdateProfileResponse>
{
    [MediatorHttpPut("/api/auth/profile", OperationId = "UpdateProfile", RequiresAuthorization = true)]
    public async Task<UpdateProfileResponse> Handle(UpdateProfileRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();

        // Serverseitige Validierung + Normalisierung (gleiche Regeln wie bei der Registrierung)
        var firstName = UserInputValidator.ValidateName(request.FirstName, "Vorname");
        var lastName = UserInputValidator.ValidateName(request.LastName, "Nachname");
        var (sellerType, companyName) = UserInputValidator.ValidateSellerInfo(request.SellerType, request.CompanyName);
        var phone = UserInputValidator.NormalizePhone(request.Phone);

        var user = await dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Benutzer nicht gefunden.");

        user.FirstName = firstName;
        user.LastName = lastName;
        user.SellerType = sellerType;
        user.CompanyName = companyName;
        user.Phone = phone;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        // Frischer Access Token, damit geaenderte Claims (Seller/SellerType) sofort wirken
        var accessToken = tokenService.GenerateAccessToken(user);

        return new UpdateProfileResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.FullName,
            accessToken,
            user.SellerType,
            user.CompanyName,
            user.Phone
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
