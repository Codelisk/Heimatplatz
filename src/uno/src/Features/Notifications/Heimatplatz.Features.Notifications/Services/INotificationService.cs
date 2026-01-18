using Heimatplatz.Features.Notifications.Contracts.Models;

namespace Heimatplatz.Features.Notifications.Services;

/// <summary>
/// Service for managing notification preferences and device registration
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Gets the current user's notification preferences
    /// </summary>
    Task<NotificationPreferenceDto> GetPreferencesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the user's notification preferences
    /// </summary>
    Task<bool> UpdatePreferencesAsync(bool isEnabled, List<string> locations, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers the current device for push notifications
    /// </summary>
    Task<bool> RegisterDeviceAsync(string deviceToken, string platform, CancellationToken cancellationToken = default);
}
