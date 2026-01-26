using Heimatplatz.Api.Features.Properties.Contracts;

namespace Heimatplatz.Api.Features.Notifications.Services;

/// <summary>
/// Service for sending push notifications
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Sends a push notification about a new property to users whose notification filter matches
    /// </summary>
    Task SendPropertyNotificationAsync(
        Guid propertyId,
        string title,
        string city,
        decimal price,
        PropertyType propertyType,
        SellerType sellerType,
        CancellationToken cancellationToken = default);
}
