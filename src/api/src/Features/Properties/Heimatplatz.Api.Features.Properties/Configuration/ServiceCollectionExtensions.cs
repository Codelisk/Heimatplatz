using Heimatplatz.Api.Core.Data.Seeding.Configuration;
using Heimatplatz.Api.Features.Properties.Data.Seeding;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Properties.Configuration;

/// <summary>
/// DI-Registrierung fuer das Properties Feature
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert alle Services des Properties Features
    /// </summary>
    public static IServiceCollection AddPropertiesFeature(this IServiceCollection services)
    {
        services.AddGeneratedServices();

        // Seeder registrieren
        services.AddSeeder<SellerSourceSeeder>();
        services.AddSeeder<PropertySeeder>();
        services.AddSeeder<FavoriteSeeder>();
        services.AddSeeder<BlockedSeeder>();

        return services;
    }
}
