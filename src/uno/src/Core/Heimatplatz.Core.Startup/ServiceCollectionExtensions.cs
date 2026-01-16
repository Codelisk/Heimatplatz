using Heimatplatz.Core.ApiClient.Configuration;
using Heimatplatz.Features.Auth.Configuration;
using Heimatplatz.Features.Properties.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Infrastructure;
using UnoFramework.Mediator;
using UnoFramework.ViewModels;

namespace Heimatplatz.Core.Startup;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        // Auto-register services with [Service] attribute
        services.AddShinyServiceRegistry();

        services.AddShinyMediator();
        services.AddSingleton<IEventCollector, UnoEventCollector>();
        services.AddSingleton<BaseServices>();

        // Core Features
        services.AddApiClientFeature();

        // Features
        services.AddAuthFeature();
        services.AddPropertiesFeature();

        return services;
    }
}
