using Heimatplatz.Api.Core.Data.Seeding.Configuration;
using Heimatplatz.Api.Features.ForeclosureAuctions.Data.Seeding;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Configuration;

/// <summary>
/// Extension-Methoden fuer Dependency Injection des ForeclosureAuctions-Features
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert alle Services des ForeclosureAuctions-Features
    /// </summary>
    public static IServiceCollection AddForeclosureAuctionsFeature(this IServiceCollection services)
    {
        // Seeder registrieren
        services.AddSeeder<ForeclosureAuctionSeeder>();

        return services;
    }
}
