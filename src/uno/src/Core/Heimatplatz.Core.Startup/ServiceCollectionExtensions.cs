using Heimatplatz.Core.ApiClient.Configuration;
using Heimatplatz.Core.DeepLink.Configuration;
using Heimatplatz.Features.Auth.Configuration;
using Heimatplatz.Features.Notifications.Configuration;
using Heimatplatz.Features.Properties.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#if __ANDROID__ || __IOS__ || __MACCATALYST__
using Shiny;
#endif
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

        // Initialize Shiny Core (IPlatform, IKeyValueStore, etc.)
        // Must be called before AddNotificationsFeature which uses Shiny.Push
#if __ANDROID__ || __IOS__ || __MACCATALYST__
        services.AddShinyUno();
#endif

        // Core Features
        services.AddApiClientFeature();
        services.AddDeepLinkFeature();

        // Features
        services.AddAuthFeature();
        services.AddNotificationsFeature();
        services.AddPropertiesFeature(configuration);

        return services;
    }
}
