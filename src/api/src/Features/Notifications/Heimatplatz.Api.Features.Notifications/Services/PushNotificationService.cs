using Fitomad.Apns;
using Fitomad.Apns.Entities.Notification;
using FirebaseAdmin.Messaging;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Notifications.Configuration;
using Heimatplatz.Api.Features.Notifications.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Notifications.Services;

/// <summary>
/// Implementation of push notification service using Firebase (Android) and APNs (iOS)
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class PushNotificationService(
    AppDbContext dbContext,
    ILogger<PushNotificationService> logger,
    IOptions<PushNotificationOptions> options,
    IApnsClient? apnsClient = null
) : IPushNotificationService
{
    private static readonly string[] AndroidPlatforms = ["Android"];
    private static readonly string[] ApplePlatforms = ["iOS", "MacCatalyst", "maccatalyst"];

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

            if (matchingPreferences.Count == 0)
            {
                logger.LogInformation("No users have notification preferences for city: {City}", city);
                return;
            }

            // Get push subscriptions for these users
            var subscriptions = await dbContext.Set<PushSubscription>()
                .Where(ps => matchingPreferences.Contains(ps.UserId))
                .ToListAsync(cancellationToken);

            if (subscriptions.Count == 0)
            {
                logger.LogInformation("No push subscriptions found for {Count} matching users", matchingPreferences.Count);
                return;
            }

            // Format notification message
            var notificationTitle = "Neue Immobilie verfügbar!";
            var notificationBody = $"{title} in {city} - CHF {price:N0}";
            var data = new Dictionary<string, string>
            {
                ["propertyId"] = propertyId.ToString(),
                ["action"] = "openProperty"
            };

            // Group subscriptions by platform
            var androidSubscriptions = subscriptions
                .Where(s => AndroidPlatforms.Contains(s.Platform, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var appleSubscriptions = subscriptions
                .Where(s => ApplePlatforms.Contains(s.Platform, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var invalidTokens = new List<PushSubscription>();

            // Send to Android via Firebase
            if (androidSubscriptions.Count > 0)
            {
                var fcmInvalidTokens = await SendFirebaseNotificationsAsync(
                    androidSubscriptions,
                    notificationTitle,
                    notificationBody,
                    data,
                    cancellationToken);
                invalidTokens.AddRange(fcmInvalidTokens);
            }

            // Send to iOS/macOS via APNs
            if (appleSubscriptions.Count > 0)
            {
                var apnsInvalidTokens = await SendApnsNotificationsAsync(
                    appleSubscriptions,
                    notificationTitle,
                    notificationBody,
                    data,
                    cancellationToken);
                invalidTokens.AddRange(apnsInvalidTokens);
            }

            // Remove invalid tokens from database
            if (invalidTokens.Count > 0)
            {
                dbContext.RemoveRange(invalidTokens);
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Removed {Count} invalid push tokens", invalidTokens.Count);
            }

            logger.LogInformation(
                "Sent push notifications for property {PropertyId} in {City}: {AndroidCount} Android, {AppleCount} Apple",
                propertyId,
                city,
                androidSubscriptions.Count,
                appleSubscriptions.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending push notifications for property {PropertyId}", propertyId);
        }
    }

    private async Task<List<PushSubscription>> SendFirebaseNotificationsAsync(
        List<PushSubscription> subscriptions,
        string title,
        string body,
        Dictionary<string, string> data,
        CancellationToken cancellationToken)
    {
        var invalidTokens = new List<PushSubscription>();

        if (FirebaseMessaging.DefaultInstance == null)
        {
            logger.LogWarning("Firebase is not configured. Skipping {Count} Android notifications", subscriptions.Count);
            return invalidTokens;
        }

        try
        {
            var tokens = subscriptions.Select(s => s.DeviceToken).ToList();

            var message = new MulticastMessage
            {
                Tokens = tokens,
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = title,
                    Body = body
                },
                Data = data,
                Android = new AndroidConfig
                {
                    Notification = new AndroidNotification
                    {
                        ClickAction = "SHINY_PUSH_NOTIFICATION_CLICK"
                    }
                }
            };

            var response = await FirebaseMessaging.DefaultInstance
                .SendEachForMulticastAsync(message, cancellationToken);

            logger.LogInformation(
                "Firebase: Sent {Success}/{Total} notifications",
                response.SuccessCount,
                subscriptions.Count);

            // Collect invalid tokens for removal
            for (int i = 0; i < response.Responses.Count; i++)
            {
                if (!response.Responses[i].IsSuccess)
                {
                    var errorCode = response.Responses[i].Exception?.MessagingErrorCode;

                    if (errorCode == MessagingErrorCode.Unregistered ||
                        errorCode == MessagingErrorCode.InvalidArgument)
                    {
                        invalidTokens.Add(subscriptions[i]);
                        logger.LogDebug(
                            "Invalid FCM token: {Token} (Error: {Error})",
                            subscriptions[i].DeviceToken[..20] + "...",
                            errorCode);
                    }
                    else
                    {
                        logger.LogWarning(
                            "Failed to send FCM notification: {Error}",
                            response.Responses[i].Exception?.Message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending Firebase notifications");
        }

        return invalidTokens;
    }

    private async Task<List<PushSubscription>> SendApnsNotificationsAsync(
        List<PushSubscription> subscriptions,
        string title,
        string body,
        Dictionary<string, string> data,
        CancellationToken cancellationToken)
    {
        var invalidTokens = new List<PushSubscription>();

        if (apnsClient == null || !options.Value.Apns.Enabled)
        {
            logger.LogWarning("APNs is not configured. Skipping {Count} Apple notifications", subscriptions.Count);
            return invalidTokens;
        }

        try
        {
            var alert = new Alert
            {
                Title = title,
                Body = body
            };

            foreach (var subscription in subscriptions)
            {
                try
                {
                    // Use Category for action type and ThreadId for propertyId
                    // Client will parse these to handle navigation
                    var notification = new NotificationBuilder()
                        .WithAlert(alert)
                        .WithCategory(data["action"])
                        .WithThreadId(data["propertyId"])
                        .Build();

                    var response = await apnsClient.SendAsync(notification, deviceToken: subscription.DeviceToken);

                    if (!response.IsSuccess)
                    {
                        var errorReason = response.Error?.Reason;

                        if (errorReason == "BadDeviceToken" ||
                            errorReason == "Unregistered" ||
                            errorReason == "ExpiredToken")
                        {
                            invalidTokens.Add(subscription);
                            logger.LogDebug(
                                "Invalid APNs token: {Token} (Reason: {Reason})",
                                subscription.DeviceToken[..Math.Min(20, subscription.DeviceToken.Length)] + "...",
                                errorReason);
                        }
                        else
                        {
                            logger.LogWarning(
                                "Failed to send APNs notification: {Reason} (Status: {StatusCode})",
                                response.Error?.Reason,
                                response.Error?.StatusCode);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error sending APNs notification to {Token}",
                        subscription.DeviceToken[..Math.Min(20, subscription.DeviceToken.Length)] + "...");
                }
            }

            logger.LogInformation(
                "APNs: Sent notifications to {Count} devices ({Invalid} invalid)",
                subscriptions.Count,
                invalidTokens.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending APNs notifications");
        }

        return invalidTokens;
    }
}
