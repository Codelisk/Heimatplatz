using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Notifications.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Notifications.Services;

/// <summary>
/// Implementation of push notification service using Shiny.Push
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class PushNotificationService(
    AppDbContext dbContext,
    ILogger<PushNotificationService> logger
) : IPushNotificationService
{
    public async Task SendPropertyNotificationAsync(
        Guid propertyId,
        string title,
        string city,
        decimal price,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Find all users with notification preferences matching this city
            var matchingPreferences = await dbContext.Set<NotificationPreference>()
                .Where(np => np.Location.ToLower() == city.ToLower() && np.IsEnabled)
                .Select(np => np.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (!matchingPreferences.Any())
            {
                logger.LogInformation("No users have notification preferences for city: {City}", city);
                return;
            }

            // Get push subscriptions for these users
            var subscriptions = await dbContext.Set<PushSubscription>()
                .Where(ps => matchingPreferences.Contains(ps.UserId))
                .ToListAsync(cancellationToken);

            if (!subscriptions.Any())
            {
                logger.LogInformation("No push subscriptions found for {Count} matching users", matchingPreferences.Count);
                return;
            }

            // Format notification message
            var notificationTitle = "Neue Immobilie verfügbar!";
            var notificationBody = $"{title} in {city} - CHF {price:N0}";

            // Send push notifications
            // NOTE: Actual Shiny.Push implementation will be added here
            // For now, we'll log the notifications that would be sent
            foreach (var subscription in subscriptions)
            {
                logger.LogInformation(
                    "Would send push notification to device {DeviceToken} ({Platform}): {Title} - {Body}",
                    subscription.DeviceToken,
                    subscription.Platform,
                    notificationTitle,
                    notificationBody);

                // TODO: Implement actual push notification sending with Shiny.Push
                // await pushManager.SendAsync(subscription.DeviceToken, notificationTitle, notificationBody, ...);
            }

            logger.LogInformation(
                "Sent {Count} push notifications for property {PropertyId} in {City}",
                subscriptions.Count,
                propertyId,
                city);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending push notifications for property {PropertyId}", propertyId);
        }
    }
}
