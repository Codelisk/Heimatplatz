using Heimatplatz.Api.Core.Data.Seeding.Configuration;
using Heimatplatz.Api.Features.WkoCompanies.Data.Seeding;
using Heimatplatz.Api.Features.WkoCompanies.Infrastructure;
using Heimatplatz.Api.Features.WkoCompanies.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.WkoCompanies.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWkoCompaniesFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddGeneratedServices();

        services.Configure<WkoScrapingOptions>(configuration.GetSection(WkoScrapingOptions.SectionName));

        // HttpClient fuer WkoCompanyScraper mit Resilience (Retry/Circuit-Breaker fuer HTTP 429,
        // das firmen.wko.at bei zu dichten Requests zurueckgibt)
        services.AddHttpClient<IWkoCompanyScraper, WkoCompanyScraper>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            client.Timeout = TimeSpan.FromSeconds(
                configuration.GetValue($"{WkoScrapingOptions.SectionName}:TimeoutSeconds", 30));
        })
        .AddStandardResilienceHandler();

        services.AddScoped<IWkoCompanySyncService, WkoCompanySyncService>();

        services.AddSeeder<WkoCompanySeeder>();

        services.AddHostedService<WkoCompanySyncWorker>();

        return services;
    }
}
