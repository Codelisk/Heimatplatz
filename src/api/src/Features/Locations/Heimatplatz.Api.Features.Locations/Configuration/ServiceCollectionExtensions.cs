using Heimatplatz.Api.Core.Data.Seeding.Configuration;
using Heimatplatz.Api.Features.Locations.Data.Seeding;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Locations.Configuration;

/// <summary>
/// DI-Registrierung fuer das Locations Feature
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert alle Services des Locations Features
    /// </summary>
    public static IServiceCollection AddLocationsFeature(this IServiceCollection services)
    {
        // HttpClient fuer OpenPLZ API Import
        services.AddHttpClient();

        // Seeder registrieren
        services.AddSeeder<LocationSeeder>();

        return services;
    }
}
