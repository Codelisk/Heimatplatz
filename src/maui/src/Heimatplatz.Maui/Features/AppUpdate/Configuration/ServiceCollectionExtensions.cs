using Heimatplatz.Features.AppUpdate.Contracts;
using Heimatplatz.Features.AppUpdate.Contracts.Mediator.Commands;
using Heimatplatz.Maui.Features.AppUpdate.Handlers;
using Heimatplatz.Maui.Features.AppUpdate.Services;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator;
#if ANDROID
using Shiny.Hosting;
#endif

namespace Heimatplatz.Maui.Features.AppUpdate.Configuration;

/// <summary>
/// Extension methods for configuring AppUpdate feature services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds AppUpdate feature services to the dependency injection container.
    /// Call from MauiProgram: builder.Services.AddAppUpdateFeature();
    /// </summary>
    public static IServiceCollection AddAppUpdateFeature(this IServiceCollection services)
    {
#if ANDROID
        services.AddSingleton<AndroidAppUpdateService>();
        services.AddSingleton<IAppUpdateService>(sp => sp.GetRequiredService<AndroidAppUpdateService>());
        // Shiny-Lifecycle-Hook: Update-Flow-Ergebnis (OnActivityResult) automatisch zustellen
        services.AddSingleton<IAndroidLifecycle.IOnActivityResult>(sp => sp.GetRequiredService<AndroidAppUpdateService>());
#else
        services.AddSingleton<IAppUpdateService, NoOpAppUpdateService>();
#endif

        // Mediator-Handler (wird von Shiny.Mediator aus dem DI-Container aufgeloest)
        services.AddSingleton<ICommandHandler<CheckForAppUpdateCommand>, CheckForAppUpdateCommandHandler>();

        return services;
    }
}
