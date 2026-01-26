using System.Net.Http.Json;
using Heimatplatz;
using Heimatplatz.Core.ApiClient;
using Heimatplatz.Features.Notifications.Contracts.Models;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;

namespace Heimatplatz.Features.Notifications.Services;

/// <summary>
/// Implementation of notification service
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class NotificationService(
    HttpClient httpClient,
    ILogger<NotificationService> logger
) : INotificationService
{
    public async Task<NotificationPreferenceDto> GetPreferencesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<GetNotificationPreferencesResponse>(
                "/api/notifications/preferences",
                cancellationToken);

            if (response == null)
            {
                logger.LogWarning("Failed to get notification preferences - null response");
                return new NotificationPreferenceDto(false, []);
            }

            return new NotificationPreferenceDto(
                response.IsEnabled,
                response.Locations,
                response.IsPrivateSelected,
                response.IsBrokerSelected,
                response.IsPortalSelected,
                response.ExcludedSellerSourceIds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting notification preferences");
            return new NotificationPreferenceDto(false, []);
        }
    }

    public async Task<bool> UpdatePreferencesAsync(
        bool isEnabled,
        List<string> locations,
        bool isPrivateSelected = true,
        bool isBrokerSelected = true,
        bool isPortalSelected = true,
        List<Guid>? excludedSellerSourceIds = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new UpdateNotificationPreferencesRequest(
                isEnabled,
                locations,
                isPrivateSelected,
                isBrokerSelected,
                isPortalSelected,
                excludedSellerSourceIds ?? []);
            var response = await httpClient.PutAsJsonAsync(
                "/api/notifications/preferences",
                request,
                cancellationToken);

            return response.IsSuccessStatusCode;
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
            var request = new RegisterDeviceRequest(deviceToken, platform);
            var response = await httpClient.PostAsJsonAsync(
                "/api/notifications/register-device",
                request,
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering device for push notifications");
            return false;
        }
    }
}

// DTOs matching API contracts
internal record GetNotificationPreferencesResponse(
    bool IsEnabled,
    List<string> Locations,
    bool IsPrivateSelected,
    bool IsBrokerSelected,
    bool IsPortalSelected,
    List<Guid> ExcludedSellerSourceIds);
internal record UpdateNotificationPreferencesRequest(
    bool IsEnabled,
    List<string> Locations,
    bool IsPrivateSelected,
    bool IsBrokerSelected,
    bool IsPortalSelected,
    List<Guid> ExcludedSellerSourceIds);
internal record RegisterDeviceRequest(string DeviceToken, string Platform);
