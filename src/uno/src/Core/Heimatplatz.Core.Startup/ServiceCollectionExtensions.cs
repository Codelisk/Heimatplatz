using Heimatplatz.Features.Immobilien.Configuration;
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

        // Features
        services.AddImmobilienFeature();

        return services;
    }
}
