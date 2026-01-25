using Heimatplatz.Core.ApiClient.Configuration;
using Heimatplatz.Features.Auth.Configuration;
using Heimatplatz.Features.Notifications.Configuration;
using Heimatplatz.Features.Properties.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UnoFramework.Mediator;
using UnoFramework.ViewModels;

namespace Heimatplatz.Core.Startup;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Auto-register services with [Service] attribute
        services.AddShinyServiceRegistry();

        // Configure Shiny Mediator with UnoEventCollector
        services.AddShinyMediator(cfg =>
        {
            cfg.AddEventCollector<UnoEventCollector>();
        });

        // Core Features
        services.AddApiClientFeature();

        // Features
        services.AddAuthFeature();
        services.AddNotificationsFeature();
        services.AddPropertiesFeature(configuration);

        return services;
    }
}
