using Heimatplatz.Features.Notifications.Contracts.Models;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;
using ApiGenerated = Heimatplatz.Maui.ApiClient.Generated;

namespace Heimatplatz.Maui.Features.Notifications.Services;

/// <summary>
/// Implementation of notification service using Shiny Mediator generated API client requests.
/// Explicitly registered in ServiceCollectionExtensions (AddNotificationsFeature).
/// </summary>
public class NotificationService(
    IMediator mediator,
    ILogger<NotificationService> logger
) : INotificationService
{
    private const string PushDeviceIdKey = "Push.DeviceId";

    public async Task<NotificationPreferenceDto> GetPreferencesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var (_, response) = await mediator.Request(
                new ApiGenerated.GetNotificationPreferencesHttpRequest(),
                cancellationToken);

            if (response == null)
            {
                logger.LogWarning("Failed to get notification preferences - null response");
                return new NotificationPreferenceDto(false, NotificationFilterMode.All, []);
            }

            return new NotificationPreferenceDto(
                response.IsEnabled,
                MapFilterMode(response.FilterMode),
                response.Locations?.ToList() ?? [],
                response.IsHausSelected,
                response.IsGrundstueckSelected,
                response.IsZwangsversteigerungSelected,
                response.IsPrivateSelected,
                response.IsBrokerSelected);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting notification preferences");
            return new NotificationPreferenceDto(false, NotificationFilterMode.All, []);
        }
    }

    public async Task<bool> UpdatePreferencesAsync(
        bool isEnabled,
        NotificationFilterMode filterMode,
        List<string> locations,
        bool isHausSelected = true,
        bool isGrundstueckSelected = true,
        bool isZwangsversteigerungSelected = true,
        bool isPrivateSelected = true,
        bool isBrokerSelected = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (_, response) = await mediator.Request(
                new ApiGenerated.UpdateNotificationPreferencesHttpRequest
                {
                    Body = new ApiGenerated.UpdateNotificationPreferencesRequest
                    {
                        IsEnabled = isEnabled,
                        FilterMode = MapFilterModeToApi(filterMode),
                        Locations = locations,
                        IsHausSelected = isHausSelected,
                        IsGrundstueckSelected = isGrundstueckSelected,
                        IsZwangsversteigerungSelected = isZwangsversteigerungSelected,
                        IsPrivateSelected = isPrivateSelected,
                        IsBrokerSelected = isBrokerSelected
                    }
                },
                cancellationToken);

            return response?.Success ?? false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating notification preferences");
            return false;
        }
    }

    public async Task<bool> RegisterDeviceAsync(string deviceToken, string platform, CancellationToken cancellationToken = default)
    {
        try
        {
            var (_, response) = await mediator.Request(
                new ApiGenerated.RegisterDeviceHttpRequest
                {
                    Body = new ApiGenerated.RegisterDeviceRequest
                    {
                        DeviceToken = deviceToken,
                        Platform = platform,
                        Environment = GetPushEnvironment(platform),
                        DeviceId = GetOrCreateDeviceId()
                    }
                },
                cancellationToken);

            return response?.Success ?? false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering device for push notifications");
            return false;
        }
    }

    private static string GetPushEnvironment(string platform)
    {
        if (!string.Equals(platform, "iOS", StringComparison.OrdinalIgnoreCase))
            return "Production";

#if DEBUG
        return "Sandbox";
#else
        return "Production";
#endif
    }

    private static string GetOrCreateDeviceId()
    {
        var deviceId = Preferences.Default.Get<string?>(PushDeviceIdKey, null);
        if (!string.IsNullOrWhiteSpace(deviceId))
            return deviceId;

        deviceId = Guid.NewGuid().ToString("N");
        Preferences.Default.Set(PushDeviceIdKey, deviceId);
        return deviceId;
    }

    private static NotificationFilterMode MapFilterMode(ApiGenerated.NotificationFilterMode apiMode) =>
        apiMode switch
        {
            ApiGenerated.NotificationFilterMode.All => NotificationFilterMode.All,
            ApiGenerated.NotificationFilterMode.SameAsSearch => NotificationFilterMode.SameAsSearch,
            ApiGenerated.NotificationFilterMode.Custom => NotificationFilterMode.Custom,
            _ => NotificationFilterMode.All
        };

    private static ApiGenerated.NotificationFilterMode MapFilterModeToApi(NotificationFilterMode mode) =>
        mode switch
        {
            NotificationFilterMode.All => ApiGenerated.NotificationFilterMode.All,
            NotificationFilterMode.SameAsSearch => ApiGenerated.NotificationFilterMode.SameAsSearch,
            NotificationFilterMode.Custom => ApiGenerated.NotificationFilterMode.Custom,
            _ => ApiGenerated.NotificationFilterMode.All
        };
}
