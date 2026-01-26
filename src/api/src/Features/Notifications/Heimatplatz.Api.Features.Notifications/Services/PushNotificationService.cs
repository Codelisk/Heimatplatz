using System.Text.Json;
using Fitomad.Apns;
using Fitomad.Apns.Entities.Notification;
using FirebaseAdmin.Messaging;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Notifications.Configuration;
using Heimatplatz.Api.Features.Notifications.Contracts;
using Heimatplatz.Api.Features.Notifications.Data.Entities;
using Heimatplatz.Api.Features.Properties.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Notifications.Services;

/// <summary>
/// Implementation of push notification service using Firebase (Android) and APNs (iOS).
/// Supports 3 filter modes: All, SameAsSearch, Custom.
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
        PropertyType propertyType,
        SellerType sellerType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Load all enabled notification preferences
            var enabledPreferences = await dbContext.Set<NotificationPreference>()
                .Where(np => np.IsEnabled)
                .ToListAsync(cancellationToken);

            if (enabledPreferences.Count == 0)
            {
                logger.LogInformation("No enabled notification preferences found");
                return;
            }

            // Step 2: Filter users based on their FilterMode
            var matchingUserIds = new List<Guid>();

            // Collect SameAsSearch user IDs to batch-load their filter preferences
            var sameAsSearchUserIds = enabledPreferences
                .Where(p => p.FilterMode == NotificationFilterMode.SameAsSearch)
                .Select(p => p.UserId)
                .ToList();

            // Batch-load UserFilterPreferences for SameAsSearch users
            Dictionary<Guid, UserFilterPreferences> userFilterPrefs = new();
            if (sameAsSearchUserIds.Count > 0)
            {
                userFilterPrefs = await dbContext.Set<UserFilterPreferences>()
                    .Where(ufp => sameAsSearchUserIds.Contains(ufp.UserId))
                    .ToDictionaryAsync(ufp => ufp.UserId, cancellationToken);
            }

            foreach (var pref in enabledPreferences)
            {
                bool matches = pref.FilterMode switch
                {
                    NotificationFilterMode.All => true,
                    NotificationFilterMode.SameAsSearch => MatchesSameAsSearch(
                        userFilterPrefs.GetValueOrDefault(pref.UserId), city, propertyType, sellerType),
                    NotificationFilterMode.Custom => MatchesCustomFilter(
                        pref, city, propertyType, sellerType),
                    _ => false
                };

                if (matches)
                {
                    matchingUserIds.Add(pref.UserId);
                }
            }

            if (matchingUserIds.Count == 0)
            {
                logger.LogInformation(
                    "No users match notification filters for property in {City} (Type={PropertyType}, Seller={SellerType})",
                    city, propertyType, sellerType);
                return;
            }

            // Step 3: Get push subscriptions for matching users
            var subscriptions = await dbContext.Set<PushSubscription>()
                .Where(ps => matchingUserIds.Contains(ps.UserId))
                .ToListAsync(cancellationToken);

            if (subscriptions.Count == 0)
            {
                logger.LogInformation("No push subscriptions found for {Count} matching users", matchingUserIds.Count);
                return;
            }

            // Step 4: Send notifications
            var notificationTitle = "Neue Immobilie verfügbar!";
            var notificationBody = $"{title} in {city} - CHF {price:N0}";
            var data = new Dictionary<string, string>
            {
                ["propertyId"] = propertyId.ToString(),
                ["action"] = "openProperty"
            };

            var androidSubscriptions = subscriptions
                .Where(s => AndroidPlatforms.Contains(s.Platform, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var appleSubscriptions = subscriptions
                .Where(s => ApplePlatforms.Contains(s.Platform, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var invalidTokens = new List<PushSubscription>();

            if (androidSubscriptions.Count > 0)
            {
                var fcmInvalidTokens = await SendFirebaseNotificationsAsync(
                    androidSubscriptions, notificationTitle, notificationBody, data, cancellationToken);
                invalidTokens.AddRange(fcmInvalidTokens);
            }

            if (appleSubscriptions.Count > 0)
            {
                var apnsInvalidTokens = await SendApnsNotificationsAsync(
                    appleSubscriptions, notificationTitle, notificationBody, data, cancellationToken);
                invalidTokens.AddRange(apnsInvalidTokens);
            }

            if (invalidTokens.Count > 0)
            {
                dbContext.RemoveRange(invalidTokens);
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Removed {Count} invalid push tokens", invalidTokens.Count);
            }

            logger.LogInformation(
                "Sent push notifications for property {PropertyId} in {City}: {MatchCount} matching users, {AndroidCount} Android, {AppleCount} Apple",
                propertyId, city, matchingUserIds.Count, androidSubscriptions.Count, appleSubscriptions.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending push notifications for property {PropertyId}", propertyId);
        }
    }

    /// <summary>
    /// Checks if a property matches the user's search filter (SameAsSearch mode)
    /// </summary>
    private bool MatchesSameAsSearch(
        UserFilterPreferences? filterPrefs,
        string city,
        PropertyType propertyType,
        SellerType sellerType)
    {
        // If user has no saved filter preferences, match all (like default filter)
        if (filterPrefs == null)
            return true;

        // Check location filter
        var selectedOrtes = JsonSerializer.Deserialize<List<string>>(filterPrefs.SelectedOrtesJson) ?? [];
        if (selectedOrtes.Count > 0 &&
            !selectedOrtes.Any(o => o.Equals(city, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // Check PropertyType filter
        if (!MatchesPropertyType(propertyType,
            filterPrefs.IsHausSelected, filterPrefs.IsGrundstueckSelected, filterPrefs.IsZwangsversteigerungSelected))
        {
            return false;
        }

        // Check SellerType filter
        if (!MatchesSellerType(sellerType,
            filterPrefs.IsPrivateSelected, filterPrefs.IsBrokerSelected, filterPrefs.IsPortalSelected))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if a property matches the custom notification filter
    /// </summary>
    private bool MatchesCustomFilter(
        NotificationPreference pref,
        string city,
        PropertyType propertyType,
        SellerType sellerType)
    {
        // Check location filter
        var selectedLocations = JsonSerializer.Deserialize<List<string>>(pref.SelectedLocationsJson) ?? [];
        if (selectedLocations.Count > 0 &&
            !selectedLocations.Any(l => l.Equals(city, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // Check PropertyType filter
        if (!MatchesPropertyType(propertyType,
            pref.IsHausSelected, pref.IsGrundstueckSelected, pref.IsZwangsversteigerungSelected))
        {
            return false;
        }

        // Check SellerType filter
        if (!MatchesSellerType(sellerType,
            pref.IsPrivateSelected, pref.IsBrokerSelected, pref.IsPortalSelected))
        {
            return false;
        }

        return true;
    }

    private static bool MatchesPropertyType(
        PropertyType type, bool isHaus, bool isGrundstueck, bool isZwangsversteigerung)
    {
        return type switch
        {
            PropertyType.House => isHaus,
            PropertyType.Land => isGrundstueck,
            PropertyType.Foreclosure => isZwangsversteigerung,
            _ => true
        };
    }

    private static bool MatchesSellerType(
        SellerType type, bool isPrivate, bool isBroker, bool isPortal)
    {
        return type switch
        {
            SellerType.Private => isPrivate,
            SellerType.Broker => isBroker,
            SellerType.Portal => isPortal,
            _ => true
        };
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
