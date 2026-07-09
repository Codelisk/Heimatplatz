namespace Heimatplatz.Features.Notifications.Contracts.Interfaces;

/// <summary>
/// Initializes push notifications and requests permissions.
/// Call InitializeAsync after user login to register for push notifications.
/// </summary>
public interface IPushNotificationInitializer
{
    /// <summary>
    /// Initializes push notifications after user login.
    /// Requests permissions and registers the device token with the API.
    /// </summary>
    Task InitializeAsync();
}
