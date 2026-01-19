using Heimatplatz.Api.Core.Data.Seeding.Configuration;
using Heimatplatz.Api.Features.Notifications.Data.Seeding;
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
        // Services and handlers are registered automatically via [Service] attributes
        // Entity configurations are discovered automatically by EF Core

        // Configure push notification providers (Firebase + APNs)
        services.AddPushProviders(configuration);

        // Register seeder
        services.AddSeeder<NotificationsSeeder>();

        return services;
    }
}
