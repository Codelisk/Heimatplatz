using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Notifications.Contracts;
using Heimatplatz.Api.Features.Notifications.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Notifications.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Handlers;

/// <summary>
/// Handler to get user's notification preferences
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/notifications")]
public class GetNotificationPreferencesHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GetNotificationPreferencesRequest, GetNotificationPreferencesResponse>
{
    [MediatorHttpGet("/preferences", OperationId = "GetNotificationPreferences")]
    public async Task<GetNotificationPreferencesResponse> Handle(
        GetNotificationPreferencesRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        // Get user ID from JWT token
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");

        var userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("User ID not found in token");

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid User ID in token");
        }

        // Get user's single notification preference row
        var preference = await dbContext.Set<NotificationPreference>()
            .FirstOrDefaultAsync(np => np.UserId == userId, cancellationToken);

        if (preference == null)
        {
            // Default: Benachrichtigungen aus, Filter wie in der Suche.
            // NICHT "All": dieser Modus benachrichtigt ueber jedes neue Objekt
            // und damit auch ueber Zwangsversteigerungen - die brauchen aber
            // ueberall ein ausdrueckliches Opt-in. SameAsSearch ohne
            // gespeicherte Suchfilter matcht genau Haus + Grundstueck.
            return new GetNotificationPreferencesResponse(
                IsEnabled: false,
                FilterMode: NotificationFilterMode.SameAsSearch,
                Locations: [],
                IsHausSelected: true,
                IsGrundstueckSelected: true,
                // Wie in der Suche: Zwangsversteigerungen sind standardmaessig aus
                IsZwangsversteigerungSelected: false,
                IsPrivateSelected: true,
                IsBrokerSelected: true
            );
        }

        var locations = JsonSerializer.Deserialize<List<string>>(preference.SelectedLocationsJson) ?? [];

        return new GetNotificationPreferencesResponse(
            IsEnabled: preference.IsEnabled,
            FilterMode: preference.FilterMode,
            Locations: locations,
            IsHausSelected: preference.IsHausSelected,
            IsGrundstueckSelected: preference.IsGrundstueckSelected,
            IsZwangsversteigerungSelected: preference.IsZwangsversteigerungSelected,
            IsPrivateSelected: preference.IsPrivateSelected,
            IsBrokerSelected: preference.IsBrokerSelected
        );
    }
}
