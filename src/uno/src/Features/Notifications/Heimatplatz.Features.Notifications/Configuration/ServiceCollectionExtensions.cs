using Heimatplatz.Features.Notifications.Contracts.Interfaces;
using Heimatplatz.Features.Notifications.Services;
using Microsoft.Extensions.DependencyInjection;
#if __ANDROID__ || __IOS__ || __MACCATALYST__
using Shiny;
using Shiny.Extensions.Stores;
#endif

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
        // Register notification services
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IPushNotificationInitializer, PushNotificationInitializer>();

#if __ANDROID__
        // Register Shiny Stores (required for Shiny.Notifications - provides ISerializer)
        services.AddShinyStores();
        // Register Shiny Push for Android
        services.AddPush<PushNotificationDelegate>();
        // Register Shiny Notifications for local notifications
        services.AddNotifications();
#elif __IOS__ || __MACCATALYST__
        // Register Shiny Stores (required for Shiny.Notifications - provides ISerializer)
        services.AddShinyStores();
        services.AddPush<PushNotificationDelegate>();
        // Register Shiny Notifications for local notifications
        services.AddNotifications();
#endif

        return services;
    }
}
