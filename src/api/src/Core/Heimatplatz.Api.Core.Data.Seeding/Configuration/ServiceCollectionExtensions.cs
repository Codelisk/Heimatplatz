using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Core.Data.Seeding.Configuration;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the seeding infrastructure. Call this in Core.Startup.
    /// </summary>
    public static IServiceCollection AddDataSeeding(this IServiceCollection services)
    {
        services.AddSingleton<SeederRunner>();
        return services;
    }

    /// <summary>
    /// Registers a seeder. Call this in each feature's ServiceCollectionExtensions.
    /// </summary>
    public static IServiceCollection AddSeeder<TSeeder>(this IServiceCollection services)
        where TSeeder : class, ISeeder
    {
        services.AddScoped<ISeeder, TSeeder>();
        return services;
    }
}
