using Heimatplatz.Api.Core.Data.Seeding.Configuration;
using Heimatplatz.Api.Features.Immobilien.Seeding;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Immobilien.Configuration;

/// <summary>
/// Extension Methods fuer die Registrierung des Immobilien-Features
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert das Immobilien-Feature und dessen Seeder
    /// </summary>
    public static IServiceCollection AddImmobilienFeature(this IServiceCollection services)
    {
        // Seeder fuer Testdaten registrieren
        services.AddSeeder<ImmobilienSeeder>();

        return services;
    }
}
