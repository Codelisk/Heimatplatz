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

        // Get user's notification preferences
        var preferences = await dbContext.Set<NotificationPreference>()
            .Where(np => np.UserId == userId)
            .ToListAsync(cancellationToken);

        var isEnabled = preferences.Any(p => p.IsEnabled);
        var locations = preferences
            .Where(p => p.IsEnabled)
            .Select(p => p.Location)
            .Distinct()
            .ToList();

        // Get seller type preferences from the first preference entry (they are the same across all locations)
        var firstPref = preferences.FirstOrDefault();
        var isPrivateSelected = firstPref?.IsPrivateSelected ?? true;
        var isBrokerSelected = firstPref?.IsBrokerSelected ?? true;
        var isPortalSelected = firstPref?.IsPortalSelected ?? true;
        var excludedSellerSourceIds = firstPref != null
            ? JsonSerializer.Deserialize<List<Guid>>(firstPref.ExcludedSellerSourceIdsJson) ?? []
            : new List<Guid>();

        return new GetNotificationPreferencesResponse(
            isEnabled,
            locations,
            isPrivateSelected,
            isBrokerSelected,
            isPortalSelected,
            excludedSellerSourceIds
        );
    }
}
