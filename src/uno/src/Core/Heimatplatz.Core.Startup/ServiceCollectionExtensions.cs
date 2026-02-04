using Heimatplatz.Core.ApiClient.Configuration;
using Heimatplatz.Core.DeepLink.Configuration;
using Heimatplatz.Features.Auth.Configuration;
using Heimatplatz.Features.Notifications.Configuration;
using Heimatplatz.Features.Properties.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Infrastructure.Impl;
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

        // Register ISerializerService BEFORE AddShinyMediator so TryAddSingleton becomes a no-op.
        // The explicit DefaultJsonTypeInfoResolver ensures JSON deserialization works in WASM
        // where the trimmer may strip the default reflection resolver.
        services.AddSingleton<ISerializerService>(new SysTextJsonSerializerService
        {
            JsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultBufferSize = 128,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            }
        });

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
