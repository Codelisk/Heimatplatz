namespace Heimatplatz.Api.Features.Notifications.Services;

/// <summary>
/// Service for sending push notifications
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Sends a push notification about a new property to users with matching location preferences
    /// </summary>
    /// <param name="propertyId">ID of the created property</param>
    /// <param name="title">Property title</param>
    /// <param name="city">City/location of the property</param>
    /// <param name="price">Price of the property</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendPropertyNotificationAsync(
        Guid propertyId,
        string title,
        string city,
        decimal price,
        CancellationToken cancellationToken = default);
}
