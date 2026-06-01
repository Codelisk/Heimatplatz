using Heimatplatz.Api.Cleanup;
using Heimatplatz.Api.Core.Data.Seeding.Configuration;
using Heimatplatz.Api.Features.Notifications.Data.Seeding;
using Heimatplatz.Api.Features.Notifications.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Notifications.Configuration;

/// <summary>
/// Extension methods for configuring Notifications feature services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Notifications feature services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddNotificationsFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddGeneratedServices();

        // Account-Loeschung: loescht Push-Subscriptions und Notification-Einstellungen des Benutzers.
        // Explizit (nicht via [Service]/TryAdd), damit IEnumerable<IUserDataEraser> alle Beitraege erhaelt.
        services.AddScoped<IUserDataEraser, NotificationsUserDataEraser>();

        // Configure push notification providers (Firebase + APNs)
        services.AddPushProviders(configuration);

        // Register seeder
        services.AddSeeder<NotificationsSeeder>();

        return services;
    }
}
