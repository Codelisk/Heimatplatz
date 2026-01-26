using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Notifications.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Notifications.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Handlers;

/// <summary>
/// Handler to update user's notification preferences (upsert single row per user)
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/notifications")]
public class UpdateNotificationPreferencesHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<UpdateNotificationPreferencesRequest, UpdateNotificationPreferencesResponse>
{
    [MediatorHttpPut("/preferences", OperationId = "UpdateNotificationPreferences")]
    public async Task<UpdateNotificationPreferencesResponse> Handle(
        UpdateNotificationPreferencesRequest request,
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

        var locationsJson = JsonSerializer.Serialize(request.Locations ?? []);
        var excludedSourcesJson = JsonSerializer.Serialize(request.ExcludedSellerSourceIds ?? []);

        // Upsert: find existing or create new
        var preference = await dbContext.Set<NotificationPreference>()
            .FirstOrDefaultAsync(np => np.UserId == userId, cancellationToken);

        if (preference == null)
        {
            preference = new NotificationPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            dbContext.Set<NotificationPreference>().Add(preference);
        }

        preference.IsEnabled = request.IsEnabled;
        preference.FilterMode = request.FilterMode;
        preference.SelectedLocationsJson = locationsJson;
        preference.IsHausSelected = request.IsHausSelected;
        preference.IsGrundstueckSelected = request.IsGrundstueckSelected;
        preference.IsZwangsversteigerungSelected = request.IsZwangsversteigerungSelected;
        preference.IsPrivateSelected = request.IsPrivateSelected;
        preference.IsBrokerSelected = request.IsBrokerSelected;
        preference.IsPortalSelected = request.IsPortalSelected;
        preference.ExcludedSellerSourceIdsJson = excludedSourcesJson;
        preference.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateNotificationPreferencesResponse(true);
    }
}
