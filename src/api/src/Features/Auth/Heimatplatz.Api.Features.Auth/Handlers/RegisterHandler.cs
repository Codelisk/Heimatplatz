using Heimatplatz.Api;
using Heimatplatz.Api.Exceptions;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Handlers;

/// <summary>
/// Handler fuer RegisterRequest - registriert neuen Benutzer und loggt automatisch ein.
/// Jeder Benutzer ist implizit Kaeufer; wer einen SellerType angibt, ist Verkaeufer.
/// Die komplette Validierung passiert hier serverseitig (Backend-First).
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
        // Serverseitige Validierung + Normalisierung
        var firstName = UserInputValidator.ValidateName(request.FirstName, "Vorname");
        var lastName = UserInputValidator.ValidateName(request.LastName, "Nachname");
        var email = UserInputValidator.NormalizeAndValidateEmail(request.Email);
        var password = UserInputValidator.ValidatePassword(request.Password);
        var (sellerType, companyName) = UserInputValidator.ValidateSellerInfo(request.SellerType, request.CompanyName);

        // Frueher Duplikat-Check fuer eine saubere Fehlermeldung (der Unique-Index
        // faengt das Race zweier paralleler Registrierungen weiter unten ab)
        var emailExists = await dbContext.Set<User>()
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (emailExists)
        {
            throw new ConflictException("Ein Benutzer mit dieser E-Mail-Adresse existiert bereits.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            SellerType = sellerType,
            CompanyName = companyName,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Automatischer Login nach Registrierung
        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshTokenString = tokenService.GenerateRefreshToken();
        var refreshValidityHours = tokenService.GetRefreshTokenValidityHours();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(refreshValidityHours);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Set<User>().Add(user);
        dbContext.Set<RefreshToken>().Add(refreshToken);

        try
        {
            // Ein einziges SaveChanges: User + Refresh-Token atomar
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Race: gleiche E-Mail wurde parallel registriert -> Unique-Index hat gegriffen.
            // Provider-neutral verifizieren statt SQLite-/SqlServer-Fehlercodes zu parsen.
            dbContext.ChangeTracker.Clear();
            var raceLost = await dbContext.Set<User>()
                .AnyAsync(u => u.Email == email, cancellationToken);

            if (raceLost)
                throw new ConflictException("Ein Benutzer mit dieser E-Mail-Adresse existiert bereits.");

            throw;
        }

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
