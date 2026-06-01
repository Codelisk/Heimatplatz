using Heimatplatz.Api.Cleanup;
using Heimatplatz.Api.Core.Data.Seeding.Configuration;
using Heimatplatz.Api.Features.Properties.Data.Seeding;
using Heimatplatz.Api.Features.Properties.Services;
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

        // Account-Loeschung: loescht Inserate, Favoriten und Blockierungen des Benutzers.
        // Explizit (nicht via [Service]/TryAdd), damit IEnumerable<IUserDataEraser> alle Beitraege erhaelt.
        services.AddScoped<IUserDataEraser, PropertiesUserDataEraser>();

        // Seeder registrieren
        services.AddSeeder<SellerSourceSeeder>();
        services.AddSeeder<PropertySeeder>();
        services.AddSeeder<FavoriteSeeder>();
        services.AddSeeder<BlockedSeeder>();

        return services;
    }
}
