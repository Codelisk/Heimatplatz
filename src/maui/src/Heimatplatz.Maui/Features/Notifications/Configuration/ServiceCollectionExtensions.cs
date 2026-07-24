using Heimatplatz.Features.Notifications.Contracts.Interfaces;
using Heimatplatz.Maui.Features.Notifications.Services;
using Microsoft.Extensions.DependencyInjection;
#if ANDROID || IOS
using Shiny;
#endif

namespace Heimatplatz.Maui.Features.Notifications.Configuration;

/// <summary>
/// Extension methods for configuring Notifications feature services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Notifications feature services to the dependency injection container.
    /// Call from MauiProgram: builder.Services.AddNotificationsFeature();
    /// </summary>
    public static IServiceCollection AddNotificationsFeature(this IServiceCollection services)
    {
        // Register notification services (explizite Singletons wie im Uno-Original)
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IPushNotificationInitializer, PushNotificationInitializer>();

#if ANDROID || IOS
        // Shiny Push (Android FCM via google-services.json, iOS APNs)
        services.AddPush<PushNotificationDelegate>();
        // Shiny Notifications fuer lokale Notifications (Android Foreground-Anzeige);
        // der Delegate navigiert beim Tap ueber den Deep-Link im Notification-Payload
        services.AddNotifications<LocalNotificationDelegate>();
#endif

        return services;
    }
}
