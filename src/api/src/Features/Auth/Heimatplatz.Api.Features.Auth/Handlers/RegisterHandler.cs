using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Handlers;

/// <summary>
/// Handler fuer RegisterRequest - registriert neuen Benutzer
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class RegisterHandler(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher
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

        // Neuen Benutzer erstellen
        var user = new User
        {
            Id = Guid.NewGuid(),
            Vorname = request.Vorname,
            Nachname = request.Nachname,
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Passwort),
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Set<User>().Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterResponse(
            user.Id,
            user.Email,
            user.FullName
        );
    }
}
