using Heimatplatz.Api.Core.Data.Seeding.Configuration;
using Heimatplatz.Api.Features.SrealListings.Data.Seeding;
using Heimatplatz.Api.Features.SrealListings.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.SrealListings.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSrealListingsFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Scraping-Konfiguration
        services.Configure<SrealScrapingOptions>(configuration.GetSection(SrealScrapingOptions.SectionName));

        // HttpClient fuer SrealScraper mit Resilience
        services.AddHttpClient<ISrealScraper, SrealScraper>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            client.Timeout = TimeSpan.FromSeconds(
                configuration.GetValue($"{SrealScrapingOptions.SectionName}:TimeoutSeconds", 30));
        })
        .AddStandardResilienceHandler();

        // SyncService
        services.AddScoped<ISrealSyncService, SrealSyncService>();

        // Property-Sync
        services.AddScoped<ISrealPropertySyncService, SrealPropertySyncService>();

        // Seeder
        services.AddSeeder<SrealListingSeeder>();

        return services;
    }
}
