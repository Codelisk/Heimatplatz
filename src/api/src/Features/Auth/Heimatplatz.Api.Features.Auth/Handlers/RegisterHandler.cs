using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Contracts.Enums;
using Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Heimatplatz.Api.Features.Properties.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Handlers;

/// <summary>
/// Handler fuer RegisterRequest - registriert neuen Benutzer und loggt automatisch ein
/// </summary>
[AllowAnonymous]
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class RegisterHandler(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService
) : IRequestHandler<RegisterRequest, RegisterResponse>
{
    [MediatorHttpPost("/api/auth/register", OperationId = "Register")]
    public async Task<RegisterResponse> Handle(RegisterRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        // Pruefen ob Email bereits existiert
        var existingUser = await dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existingUser != null)
        {
            throw new InvalidOperationException("Ein Benutzer mit dieser E-Mail-Adresse existiert bereits.");
        }

        // Validierung: Wenn Seller-Rolle, muss SellerType angegeben werden
        var isSeller = request.Roles?.Contains(UserRoleType.Seller) == true;
        if (isSeller && request.SellerType == null)
        {
            throw new InvalidOperationException("Als Verkaeufer muss ein Verkaeufertyp angegeben werden.");
        }

        // Validierung: Wenn Broker, muss Firmenname angegeben werden
        if (request.SellerType == SellerType.Broker && string.IsNullOrWhiteSpace(request.CompanyName))
        {
            throw new InvalidOperationException("Als Makler/Agentur muss ein Firmenname angegeben werden.");
        }

        // Neuen Benutzer erstellen
        var user = new User
        {
            Id = Guid.NewGuid(),
            Vorname = request.Vorname,
            Nachname = request.Nachname,
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Passwort),
            SellerType = isSeller ? request.SellerType : null,
            CompanyName = request.SellerType == SellerType.Broker ? request.CompanyName : null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Set<User>().Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Rollen zuweisen falls angegeben
        var assignedRoles = new List<UserRoleType>();
        if (request.Roles is { Count: > 0 })
        {
            foreach (var roleType in request.Roles.Distinct())
            {
                var userRole = new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RoleType = roleType,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                dbContext.Set<UserRole>().Add(userRole);
                assignedRoles.Add(roleType);
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Automatischer Login nach Registrierung: Tokens generieren (mit zugewiesenen Rollen)
        var accessToken = tokenService.GenerateAccessToken(user, roles: assignedRoles.Count > 0 ? assignedRoles : null);
        var refreshTokenString = tokenService.GenerateRefreshToken();
        var refreshValidityHours = tokenService.GetRefreshTokenValidityHours();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(refreshValidityHours);

        // Refresh Token in DB speichern
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Set<RefreshToken>().Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterResponse(
            accessToken,
            refreshTokenString,
            user.Id,
            user.Email,
            user.FullName,
            expiresAt
        );
    }
}
