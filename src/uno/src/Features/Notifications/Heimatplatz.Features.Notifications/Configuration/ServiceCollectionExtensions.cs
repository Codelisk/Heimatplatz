using Heimatplatz.Features.Notifications.Contracts.Interfaces;
using Heimatplatz.Features.Notifications.Services;
using Microsoft.Extensions.DependencyInjection;
#if __ANDROID__ || __IOS__ || __MACCATALYST__
using Shiny;
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
        // Explicitly register services
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IPushNotificationInitializer, PushNotificationInitializer>();

#if __ANDROID__ || __IOS__ || __MACCATALYST__
        // Register Shiny Push with our delegate for handling notifications
        // This uses Shiny 4.0's simplified API that automatically registers:
        // - IPlatform (AndroidPlatform/ApplePlatform)
        // - IPushManager (PushManager)
        // - FirebaseConfig (from google-services.json on Android)
        services.AddPush<PushNotificationDelegate>();
#endif

        return services;
    }
}
