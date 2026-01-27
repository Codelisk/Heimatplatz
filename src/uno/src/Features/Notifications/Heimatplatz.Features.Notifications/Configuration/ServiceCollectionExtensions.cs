using Microsoft.Extensions.DependencyInjection;
#if __ANDROID__
using Heimatplatz.Features.Notifications.Services;
using Shiny;
#elif __IOS__ || __MACCATALYST__
using Heimatplatz.Features.Notifications.Services;
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
        // Services are registered automatically via [Service] attributes
        // ViewModels and Pages are registered via MVUX source generation

#if __ANDROID__
        // Register AndroidPlatform for Shiny.Push (required dependency)
        // Shiny 4.0 requires this to be registered manually when not using Shiny.Hosting.Maui
        services.AddSingleton<AndroidPlatform>();

        // Register Shiny.Push with Firebase (uses google-services.json in Platforms/Android/)
        services.AddPush<PushNotificationDelegate>();
#elif __IOS__ || __MACCATALYST__
        // Register Shiny.Push with native APNs
        services.AddPush<PushNotificationDelegate>();
#endif

        return services;
    }
}
