namespace Heimatplatz.Features.Notifications.Contracts.Interfaces;

/// <summary>
/// Initializes push notifications and requests permissions.
/// Call InitializeAsync after user login or when notifications are explicitly enabled
/// to register the current device for push notifications.
/// </summary>
public interface IPushNotificationInitializer
{
    /// <summary>
    /// Initializes push notifications for the authenticated user.
    /// Requests permissions and registers the device token with the API.
    /// </summary>
    Task InitializeAsync();
}
