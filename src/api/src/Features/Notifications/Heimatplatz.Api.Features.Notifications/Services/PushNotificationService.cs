using System.Text.Json;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Notifications.Contracts;
using Heimatplatz.Api.Features.Notifications.Data.Entities;
using Heimatplatz.Api.Features.Properties.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Extensions.Push;

namespace Heimatplatz.Api.Features.Notifications.Services;

/// <summary>
/// Implements Heimatplatz targeting rules and dispatches through Shiny.Extensions.Push.
/// Supports 3 filter modes: All, SameAsSearch, Custom.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class PushNotificationService(
    AppDbContext dbContext,
    ILogger<PushNotificationService> logger,
    IPushManager pushManager
) : IPushNotificationService
{
    private const string ShinyAndroidClickAction = "SHINY_PUSH_NOTIFICATION_CLICK";

    public async Task SendPropertyNotificationAsync(
        Guid propertyId,
        string title,
        string city,
        decimal price,
        PropertyType propertyType,
        SellerType sellerType,
        bool isNewBuildProject = false,
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
                    // "Alle" pusht bewusst auch Neubauprojekte (wie bei ZV: der Modus
                    // heisst "alle neuen Objekte" und ignoriert die Suchfilter komplett)
                    NotificationFilterMode.All => true,
                    NotificationFilterMode.SameAsSearch => MatchesSameAsSearch(
                        userFilterPrefs.GetValueOrDefault(pref.UserId), city, propertyType, sellerType, isNewBuildProject),
                    NotificationFilterMode.Custom => MatchesCustomFilter(
                        pref, city, propertyType, sellerType, isNewBuildProject),
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
            var notificationBody = $"{title} in {city} - € {price:N0}";
            var deepLink = $"heimatplatz://property/{propertyId}";
            var data = new Dictionary<string, string>
            {
                ["propertyId"] = propertyId.ToString(),
                ["action"] = "openProperty",
                // Keep both spellings while MAUI/legacy clients converge on one payload contract.
                ["deepLink"] = deepLink,
                ["deeplink"] = deepLink
            };

            var notification = new Shiny.Extensions.Push.PushNotification
            {
                Title = notificationTitle,
                Message = notificationBody,
                // Shiny.Push for Android routes notification taps through this intent action.
                // The actual application URI is carried in Data for both FCM and APNs.
                DeepLink = ShinyAndroidClickAction,
                Sound = "default",
                Data = data,
                Apple = new ApplePushOptions
                {
                    Category = "openProperty",
                    ThreadId = propertyId.ToString()
                }
            };

            var result = await pushManager.SendToTokens(
                subscriptions.Select(x => x.DeviceToken),
                notification,
                cancellationToken);

            logger.LogInformation(
                "Push batch {BatchId} for property {PropertyId} in {City}: {Sent}/{Total} sent, {Failed} failed, {Pruned} pruned",
                result.BatchId,
                propertyId,
                city,
                result.Sent,
                result.Total,
                result.Failed,
                result.TokensRemoved);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending push notifications for property {PropertyId}", propertyId);
        }
    }

    /// <inheritdoc />
    public async Task SendFeedbackReplyNotificationAsync(
        Guid ticketId,
        Guid userId,
        string subject,
        string bodyPreview,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriptions = await dbContext.Set<PushSubscription>()
                .Where(ps => ps.UserId == userId)
                .ToListAsync(cancellationToken);

            if (subscriptions.Count == 0)
            {
                logger.LogInformation("No push subscriptions for user {UserId} - feedback reply for ticket {TicketId} not pushed", userId, ticketId);
                return;
            }

            var deepLink = $"heimatplatz://feedback/{ticketId}";
            var notification = new Shiny.Extensions.Push.PushNotification
            {
                Title = string.IsNullOrWhiteSpace(subject)
                    ? "Antwort auf dein Feedback"
                    : $"Antwort auf: {subject}",
                Message = bodyPreview,
                // Shiny.Push for Android routes notification taps through this intent action.
                // The actual application URI is carried in Data for both FCM and APNs.
                DeepLink = ShinyAndroidClickAction,
                Sound = "default",
                Data = new Dictionary<string, string>
                {
                    ["ticketId"] = ticketId.ToString(),
                    ["action"] = "openFeedback",
                    // Keep both spellings while MAUI/legacy clients converge on one payload contract.
                    ["deepLink"] = deepLink,
                    ["deeplink"] = deepLink
                },
                Apple = new ApplePushOptions
                {
                    Category = "openFeedback",
                    ThreadId = ticketId.ToString()
                }
            };

            var result = await pushManager.SendToTokens(
                subscriptions.Select(x => x.DeviceToken),
                notification,
                cancellationToken);

            logger.LogInformation(
                "Push batch {BatchId} for feedback reply on ticket {TicketId}: {Sent}/{Total} sent, {Failed} failed, {Pruned} pruned",
                result.BatchId,
                ticketId,
                result.Sent,
                result.Total,
                result.Failed,
                result.TokensRemoved);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending feedback reply push for ticket {TicketId}", ticketId);
        }
    }

    /// <summary>
    /// Checks if a property matches the user's search filter (SameAsSearch mode)
    /// </summary>
    private bool MatchesSameAsSearch(
        UserFilterPreferences? filterPrefs,
        string city,
        PropertyType propertyType,
        SellerType sellerType,
        bool isNewBuildProject)
    {
        // If user has no saved filter preferences, match the default filter:
        // Haus + Grundstueck, Zwangsversteigerungen sind standardmaessig deaktiviert
        // (Neubauprojekte sind per Default sichtbar - kein Check noetig)
        if (filterPrefs == null)
            return propertyType != PropertyType.Foreclosure;

        // Neubauprojekt-Opt-out der Suche gilt auch fuer Push
        if (isNewBuildProject && !filterPrefs.IsNeubauprojektSelected)
            return false;

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
            filterPrefs.IsPrivateSelected, filterPrefs.IsBrokerSelected))
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
        SellerType sellerType,
        bool isNewBuildProject)
    {
        // Neubauprojekt-Opt-out (eigenes Feld im Custom-Filter, Default an)
        if (isNewBuildProject && !pref.IsNeubauprojektSelected)
            return false;

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
            pref.IsPrivateSelected, pref.IsBrokerSelected))
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
        SellerType type, bool isPrivate, bool isBroker)
    {
        return type switch
        {
            SellerType.Private => isPrivate,
            SellerType.Broker => isBroker,
            // Hausverwaltungen sind gewerbliche Anbieter - die Praeferenzen kennen nur
            // privat/gewerblich, deshalb greift hier das Makler-Flag
            SellerType.PropertyManager => isBroker,
            _ => true
        };
    }
}
