using Heimatplatz.Api.Core.Data.Seeding.Configuration;
using Heimatplatz.Api.Features.ForeclosureAuctions.Data.Seeding;
using Heimatplatz.Api.Features.ForeclosureAuctions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddForeclosureAuctionsFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Scraping-Konfiguration
        services.Configure<ScrapingOptions>(configuration.GetSection(ScrapingOptions.SectionName));

        // HttpClient fuer EdikteScraper mit Resilience
        services.AddHttpClient<IEdikteScraper, EdikteScraper>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            client.Timeout = TimeSpan.FromSeconds(
                configuration.GetValue($"{ScrapingOptions.SectionName}:TimeoutSeconds", 30));
        })
        .AddStandardResilienceHandler();

        // SyncService
        services.AddScoped<IForeclosureAuctionSyncService, ForeclosureAuctionSyncService>();

        // Property-Sync
        services.AddScoped<IForeclosurePropertySyncService, ForeclosurePropertySyncService>();

        // Seeder
        services.AddSeeder<SystemUserSeeder>();
        services.AddSeeder<ForeclosureAuctionSeeder>();

        return services;
    }
}
