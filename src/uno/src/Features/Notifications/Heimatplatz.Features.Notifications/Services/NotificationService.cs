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
                return new NotificationPreferenceDto(false, new List<string>());
            }

            return new NotificationPreferenceDto(response.IsEnabled, response.Locations);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting notification preferences");
            return new NotificationPreferenceDto(false, new List<string>());
        }
    }

    public async Task<bool> UpdatePreferencesAsync(bool isEnabled, List<string> locations, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new UpdateNotificationPreferencesRequest(isEnabled, locations);
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
internal record GetNotificationPreferencesResponse(bool IsEnabled, List<string> Locations);
internal record UpdateNotificationPreferencesRequest(bool IsEnabled, List<string> Locations);
internal record RegisterDeviceRequest(string DeviceToken, string Platform);
