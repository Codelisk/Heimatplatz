using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Features.Notifications.Configuration;

/// <summary>
/// Extension methods for configuring Notifications feature services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Notifications feature services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddNotificationsFeature(this IServiceCollection services)
    {
        // Services are registered automatically via [Service] attributes:
        // - IFirebasePushManager / FirebasePushManager
        // - IPushNotificationInitializer / PushNotificationInitializer
        // - INotificationService / NotificationService

        return services;
    }
}
