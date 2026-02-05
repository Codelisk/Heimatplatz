using Heimatplatz.Features.AppUpdate.Contracts;
using Heimatplatz.Features.AppUpdate.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Features.AppUpdate.Configuration;

/// <summary>
/// Extension methods for configuring AppUpdate feature services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds AppUpdate feature services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddAppUpdateFeature(this IServiceCollection services)
    {
#if __ANDROID__
        services.AddSingleton<IAppUpdateService, Platforms.Android.AndroidAppUpdateService>();
#else
        services.AddSingleton<IAppUpdateService, NoOpAppUpdateService>();
#endif
        return services;
    }
}
