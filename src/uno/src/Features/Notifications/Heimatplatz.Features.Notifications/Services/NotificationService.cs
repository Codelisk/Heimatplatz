using Heimatplatz.Features.Notifications.Contracts.Models;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;
using ApiGenerated = Heimatplatz.Core.ApiClient.Generated;

namespace Heimatplatz.Features.Notifications.Services;

/// <summary>
/// Implementation of notification service using Shiny Mediator generated API client requests
/// Explicitly registered in ServiceCollectionExtensions
/// </summary>
public class NotificationService(
    IMediator mediator,
    ILogger<NotificationService> logger
) : INotificationService
{
    public async Task<NotificationPreferenceDto> GetPreferencesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var (context, response) = await mediator.Request(
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
                response.IsBrokerSelected,
                response.IsPortalSelected,
                response.ExcludedSellerSourceIds?.ToList() ?? []);
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
        bool isPortalSelected = true,
        List<Guid>? excludedSellerSourceIds = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (context, response) = await mediator.Request(
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
                        IsBrokerSelected = isBrokerSelected,
                        IsPortalSelected = isPortalSelected,
                        ExcludedSellerSourceIds = excludedSellerSourceIds ?? []
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
            var (context, response) = await mediator.Request(
                new ApiGenerated.RegisterDeviceHttpRequest
                {
                    Body = new ApiGenerated.RegisterDeviceRequest
                    {
                        DeviceToken = deviceToken,
                        Platform = platform
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
