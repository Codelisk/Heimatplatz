using Heimatplatz.Api.Core.Data.Seeding.Configuration;
using Heimatplatz.Api.Features.Legal.Data.Seeding;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Legal.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLegalFeature(this IServiceCollection services)
    {
        // Seeder registrieren
        services.AddSeeder<LegalSettingsSeeder>();

        return services;
    }
}
