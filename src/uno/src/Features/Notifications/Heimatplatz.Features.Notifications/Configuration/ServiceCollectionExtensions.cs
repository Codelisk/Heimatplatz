using Microsoft.Extensions.DependencyInjection;
#if __ANDROID__ || __IOS__ || __MACCATALYST__
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

#if __ANDROID__ || __IOS__ || __MACCATALYST__
        // Register Shiny.Push with native providers
        // Android: Uses Firebase via google-services.json (place in Platforms/Android/)
        // iOS/macOS: Uses native APNs
        services.AddPush<PushNotificationDelegate>();
#endif

        return services;
    }
}
