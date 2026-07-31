using Heimatplatz.Api.Core.AiConnectorClient.Configuration;
using Heimatplatz.Api.Core.Data.Seeding.Configuration;
using Heimatplatz.Api.Features.Marketing.Data.Seeding;
using Heimatplatz.Api.Features.Marketing.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Marketing.Configuration;

/// <summary>
/// DI-Registrierung fuer das Marketing Feature
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert die Services des Marketing Features: KI-gestuetzte E-Mail-Erstellung
    /// (Provider Mock/AiConnector je nach Konfiguration) und Versand mit Signatur.
    /// </summary>
    public static IServiceCollection AddMarketingFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MarketingOptions>(configuration.GetSection(MarketingOptions.SectionName));
        services.AddGeneratedServices();
        services.AddSeeder<MarketingSeeder>();
        // Referenzdaten - laeuft auch in Produktion, sonst waere die Vorlagen-Auswahl leer
        services.AddSeeder<MarketingTemplateSeeder>();

        // KI-Provider (Generierung + Entwurfs-Pruefung) je nach Konfiguration
        // (Mock = Dev-Platzhalter ohne KI, AiConnector = externer KI-Backend-Service)
        var options = configuration.GetSection(MarketingOptions.SectionName).Get<MarketingOptions>() ?? new MarketingOptions();
        if (string.Equals(options.Provider, "AiConnector", StringComparison.OrdinalIgnoreCase))
        {
            services.AddAiConnectorClient(configuration);
            services.AddScoped<IMarketingEmailGenerator, AiConnectorMarketingEmailGenerator>();
            services.AddScoped<IMarketingReplyChecker, AiConnectorMarketingReplyChecker>();
        }
        else
        {
            services.AddScoped<IMarketingEmailGenerator, MockMarketingEmailGenerator>();
            services.AddScoped<IMarketingReplyChecker, MockMarketingReplyChecker>();
        }

        return services;
    }
}
