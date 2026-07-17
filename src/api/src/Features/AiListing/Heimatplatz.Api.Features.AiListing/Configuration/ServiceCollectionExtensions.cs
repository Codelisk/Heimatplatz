using Heimatplatz.Api.Core.AiConnectorClient.Configuration;
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
    /// Registriert die Services des AiListing Features: Medien-Upload und die
    /// Beschreibungs-Generierung (Provider Mock/AiConnector je nach Konfiguration).
    /// </summary>
    public static IServiceCollection AddAiListingFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiListingOptions>(configuration.GetSection(AiListingOptions.SectionName));
        services.AddGeneratedServices();

        // Beschreibungs-Provider je nach Konfiguration
        // (Mock = Dev-Template ohne KI, AiConnector = externer KI-Backend-Service)
        var options = configuration.GetSection(AiListingOptions.SectionName).Get<AiListingOptions>() ?? new AiListingOptions();
        if (string.Equals(options.Provider, "AiConnector", StringComparison.OrdinalIgnoreCase))
        {
            services.AddAiConnectorClient(configuration);
            services.AddScoped<IListingDescriptionService, AiConnectorListingDescriptionService>();
        }
        else
        {
            services.AddScoped<IListingDescriptionService, MockListingDescriptionService>();
        }

        return services;
    }
}
