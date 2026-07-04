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

        // Extraktions-Provider je nach Konfiguration (Mock = Dev, Cli = Server mit Agent-CLI)
        var options = configuration.GetSection(AiListingOptions.SectionName).Get<AiListingOptions>() ?? new AiListingOptions();
        if (string.Equals(options.Provider, "Cli", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IListingExtractionService, CliListingExtractionService>();
        else
            services.AddScoped<IListingExtractionService, MockListingExtractionService>();

        services.AddSingleton<ListingAnalysisQueue>();
        services.AddHostedService<ListingAnalysisWorker>();

        return services;
    }
}
