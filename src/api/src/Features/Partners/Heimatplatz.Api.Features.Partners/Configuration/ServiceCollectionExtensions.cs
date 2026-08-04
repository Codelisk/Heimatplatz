using Heimatplatz.Api.Core.Data.Seeding.Configuration;
using Heimatplatz.Api.Features.Partners.Data.Seeding;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Partners.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPartnersFeature(this IServiceCollection services)
    {
        services.AddGeneratedServices();

        // Demo-Seeder (laeuft nur mit Database:EnableSeeding, nie in Produktion)
        services.AddSeeder<PartnersDemoSeeder>();

        return services;
    }
}
