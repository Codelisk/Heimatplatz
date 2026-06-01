using System.IdentityModel.Tokens.Jwt;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Cleanup;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Handlers;

/// <summary>
/// Handler fuer DeleteAccountRequest - loescht das Konto des authentifizierten Benutzers
/// vollstaendig und unwiderruflich (Apple Guideline 5.1.1(v) / DSGVO Art. 17).
///
/// Die gesamte Logik liegt serverseitig: Alle Feature-eigenen Daten werden ueber die
/// registrierten <see cref="IUserDataEraser"/> entfernt, danach die Auth-eigenen Daten
/// und zuletzt der Benutzer selbst - alles innerhalb einer Transaktion. Es wird bewusst
/// explizit geloescht (ExecuteDelete in FK-sicherer Reihenfolge) statt auf DB-Cascade
/// zu vertrauen, da das Schema teils via EnsureCreated initialisiert wird.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class DeleteAccountHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IEnumerable<IUserDataEraser> userDataErasers,
    ILogger<DeleteAccountHandler> logger
) : IRequestHandler<DeleteAccountRequest, DeleteAccountResponse>
{
    [MediatorHttpDelete("/api/auth/account", OperationId = "DeleteAccount", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireAnyRole])]
    public async Task<DeleteAccountResponse> Handle(DeleteAccountRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();

        var user = await dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Benutzer nicht gefunden.");
        }

        logger.LogInformation("[DeleteAccount] Starte Loeschung von Konto {UserId} ({Email})", userId, user.Email);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            // 1. Feature-eigene Daten loeschen (jedes Feature kennt nur seine eigenen Entities).
            //    Order steuert die FK-sichere Reihenfolge ueber Feature-Grenzen hinweg.
            foreach (var eraser in userDataErasers.OrderBy(e => e.Order))
            {
                await eraser.EraseUserDataAsync(userId, cancellationToken);
            }

            // 2. Auth-eigene Daten des Benutzers
            await dbContext.Set<RefreshToken>()
                .Where(t => t.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.Set<UserRole>()
                .Where(r => r.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.Set<UserFilterPreferences>()
                .Where(p => p.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            // 3. Der Benutzer selbst
            await dbContext.Set<User>()
                .Where(u => u.Id == userId)
                .ExecuteDeleteAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });

        logger.LogInformation("[DeleteAccount] Konto {UserId} wurde vollstaendig geloescht", userId);

        return new DeleteAccountResponse(true);
    }

    private Guid GetAuthenticatedUserId()
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new UnauthorizedAccessException("Nicht authentifiziert.");

        var userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Benutzer-ID nicht gefunden.");

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Ungueltige Benutzer-ID.");
        }

        return userId;
    }
}
