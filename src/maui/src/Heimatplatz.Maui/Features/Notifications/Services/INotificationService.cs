using Heimatplatz.Features.Notifications.Contracts.Models;

namespace Heimatplatz.Maui.Features.Notifications.Services;

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
    Task<bool> UpdatePreferencesAsync(
        bool isEnabled,
        NotificationFilterMode filterMode,
        List<string> locations,
        bool isHausSelected = true,
        bool isGrundstueckSelected = true,
        // Zwangsversteigerungen sind ueberall default-aus (wie in der Suche)
        bool isZwangsversteigerungSelected = false,
        bool isPrivateSelected = true,
        bool isBrokerSelected = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers the current device for push notifications
    /// </summary>
    Task<bool> RegisterDeviceAsync(string deviceToken, string platform, CancellationToken cancellationToken = default);
}
