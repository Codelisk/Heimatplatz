using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Handlers;

/// <summary>
/// Handler fuer SaveUserFilterPreferencesRequest - speichert die Filtereinstellungen des authentifizierten Benutzers
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/auth/filter-preferences")]
public class SaveUserFilterPreferencesHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<SaveUserFilterPreferencesRequest, SaveUserFilterPreferencesResponse>
{
    [MediatorHttpPost("/", OperationId = "SaveUserFilterPreferences", AuthorizationPolicies = [AuthorizationPolicies.RequireAnyRole])]
    public async Task<SaveUserFilterPreferencesResponse> Handle(
        SaveUserFilterPreferencesRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();

        // Existierende Einstellungen suchen
        var preferences = await dbContext.Set<UserFilterPreferences>()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        // Liste zu JSON konvertieren
        var ortesJson = JsonSerializer.Serialize(request.SelectedOrtes ?? []);

        if (preferences == null)
        {
            // Neu anlegen
            preferences = new UserFilterPreferences
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SelectedOrtesJson = ortesJson,
                SelectedAgeFilter = request.SelectedAgeFilter,
                IsHausSelected = request.IsHausSelected,
                IsGrundstueckSelected = request.IsGrundstueckSelected,
                IsZwangsversteigerungSelected = request.IsZwangsversteigerungSelected,
                CreatedAt = DateTimeOffset.UtcNow
            };

            dbContext.Set<UserFilterPreferences>().Add(preferences);
        }
        else
        {
            // Aktualisieren
            preferences.SelectedOrtesJson = ortesJson;
            preferences.SelectedAgeFilter = request.SelectedAgeFilter;
            preferences.IsHausSelected = request.IsHausSelected;
            preferences.IsGrundstueckSelected = request.IsGrundstueckSelected;
            preferences.IsZwangsversteigerungSelected = request.IsZwangsversteigerungSelected;
            preferences.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SaveUserFilterPreferencesResponse(Success: true);
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
