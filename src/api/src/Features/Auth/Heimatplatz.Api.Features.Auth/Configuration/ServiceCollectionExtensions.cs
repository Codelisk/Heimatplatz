using Heimatplatz.Api.Core.Data.Seeding.Configuration;
using Heimatplatz.Api.Features.Auth.Data.Seeding;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Auth.Configuration;

/// <summary>
/// DI-Registrierung fuer das Auth Feature
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert alle Services des Auth Features
    /// </summary>
    public static IServiceCollection AddAuthFeature(this IServiceCollection services)
    {
        // Handler und Services werden automatisch via [Service] Attribut und AddShinyServiceRegistry() registriert

        // Seeder registrieren
        services.AddSeeder<UserSeeder>();

        return services;
    }
}
