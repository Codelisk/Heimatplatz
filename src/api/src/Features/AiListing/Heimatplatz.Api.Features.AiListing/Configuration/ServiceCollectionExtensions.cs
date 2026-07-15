using Heimatplatz.Api.Features.AiListing.Infrastructure;
using Heimatplatz.Api.Features.AiListing.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.AiListing.Configuration;

/// <summary>
/// DI-Registrierung fuer das AiListing Feature
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert alle Services des AiListing Features:
    /// Media-Service, Extraktions-Provider (Cli/Mock je nach Konfiguration),
    /// Job-Queue und Hintergrund-Worker.
    /// </summary>
    public static IServiceCollection AddAiListingFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiListingOptions>(configuration.GetSection(AiListingOptions.SectionName));
        services.AddGeneratedServices();

        // Extraktions-Provider je nach Konfiguration
        // (Mock = Dev, Cli = Server mit Agent-CLI, AiConnector = externer KI-Backend-Service)
        var options = configuration.GetSection(AiListingOptions.SectionName).Get<AiListingOptions>() ?? new AiListingOptions();
        if (string.Equals(options.Provider, "AiConnector", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IListingExtractionService, AiConnectorListingExtractionService>(client =>
            {
                client.BaseAddress = new Uri(options.AiConnector.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                if (!string.IsNullOrWhiteSpace(options.AiConnector.ApiKey))
                    client.DefaultRequestHeaders.Add("X-Api-Key", options.AiConnector.ApiKey);
            });
        }
        else if (string.Equals(options.Provider, "Cli", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IListingExtractionService, CliListingExtractionService>();
        else
            services.AddScoped<IListingExtractionService, MockListingExtractionService>();

        services.AddSingleton<ListingAnalysisQueue>();
        services.AddHostedService<ListingAnalysisWorker>();

        return services;
    }
}
