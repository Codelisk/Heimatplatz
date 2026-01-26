using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
/// Handler to update user's notification preferences
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

        // Remove all existing preferences for this user
        var existingPreferences = await dbContext.Set<NotificationPreference>()
            .Where(np => np.UserId == userId)
            .ToListAsync(cancellationToken);

        dbContext.Set<NotificationPreference>().RemoveRange(existingPreferences);

        // Add new preferences
        var excludedSourcesJson = JsonSerializer.Serialize(request.ExcludedSellerSourceIds ?? []);

        if (request.IsEnabled && request.Locations.Any())
        {
            foreach (var location in request.Locations.Distinct())
            {
                var preference = new NotificationPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Location = location.Trim(),
                    IsEnabled = true,
                    IsPrivateSelected = request.IsPrivateSelected,
                    IsBrokerSelected = request.IsBrokerSelected,
                    IsPortalSelected = request.IsPortalSelected,
                    ExcludedSellerSourceIdsJson = excludedSourcesJson,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                dbContext.Set<NotificationPreference>().Add(preference);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateNotificationPreferencesResponse(true);
    }
}
