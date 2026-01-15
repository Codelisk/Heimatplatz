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
        // Handler werden automatisch via [MediatorScoped] Attribut und AddMediatorRegistry() registriert

        // Seeder registrieren
        services.AddSeeder<PropertySeeder>();

        return services;
    }
}
