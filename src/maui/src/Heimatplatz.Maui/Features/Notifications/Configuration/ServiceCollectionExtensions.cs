using Heimatplatz.Features.Notifications.Contracts.Interfaces;
using Heimatplatz.Features.Notifications.Contracts.Mediator.Commands;
using Heimatplatz.Maui.Events;
using Heimatplatz.Maui.Features.Notifications.Handlers;
using Heimatplatz.Maui.Features.Notifications.Services;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator;
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

        // Mediator-Handler (werden von Shiny.Mediator aus dem DI-Container aufgeloest)
        services.AddSingleton<ICommandHandler<InitializePushNotificationsCommand>, InitializePushNotificationsCommandHandler>();
        services.AddSingleton<IEventHandler<UserLoggedInEvent>, UserLoggedInEventHandler>();

#if ANDROID || IOS
        // Shiny Push (Android FCM via google-services.json, iOS APNs)
        services.AddPush<PushNotificationDelegate>();
        // Shiny Notifications fuer lokale Notifications (Android Foreground-Anzeige)
        services.AddNotifications();
#endif

        return services;
    }
}
