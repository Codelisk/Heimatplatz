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
/// Handler fuer GetUserFilterPreferencesRequest - laedt die Filtereinstellungen des authentifizierten Benutzers
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/auth/filter-preferences")]
public class GetUserFilterPreferencesHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GetUserFilterPreferencesRequest, GetUserFilterPreferencesResponse>
{
    [MediatorHttpGet("/", OperationId = "GetUserFilterPreferences", AuthorizationPolicies = [AuthorizationPolicies.RequireAnyRole])]
    public async Task<GetUserFilterPreferencesResponse> Handle(
        GetUserFilterPreferencesRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();

        var preferences = await dbContext.Set<UserFilterPreferences>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (preferences == null)
        {
            // Defaults zurueckgeben wenn keine Einstellungen vorhanden
            return new GetUserFilterPreferencesResponse(
                SelectedOrtes: [],
                SelectedAgeFilter: 0,
                IsHausSelected: true,
                IsGrundstueckSelected: true,
                IsZwangsversteigerungSelected: true,
                IsPrivateSelected: true,
                IsBrokerSelected: true,
                IsPortalSelected: true,
                ExcludedSellerSourceIds: []
            );
        }

        // JSON zu Liste konvertieren
        var selectedOrtes = JsonSerializer.Deserialize<List<string>>(preferences.SelectedOrtesJson) ?? [];
        var excludedSellerSourceIds = JsonSerializer.Deserialize<List<Guid>>(preferences.ExcludedSellerSourceIdsJson) ?? [];

        return new GetUserFilterPreferencesResponse(
            SelectedOrtes: selectedOrtes,
            SelectedAgeFilter: preferences.SelectedAgeFilter,
            IsHausSelected: preferences.IsHausSelected,
            IsGrundstueckSelected: preferences.IsGrundstueckSelected,
            IsZwangsversteigerungSelected: preferences.IsZwangsversteigerungSelected,
            IsPrivateSelected: preferences.IsPrivateSelected,
            IsBrokerSelected: preferences.IsBrokerSelected,
            IsPortalSelected: preferences.IsPortalSelected,
            ExcludedSellerSourceIds: excludedSellerSourceIds
        );
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
