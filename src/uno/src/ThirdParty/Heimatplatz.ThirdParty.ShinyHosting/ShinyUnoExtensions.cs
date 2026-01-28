using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny;

/// <summary>
/// Extension methods for integrating Shiny with Uno Platform applications.
/// This is the Uno Platform equivalent of Shiny.Hosting.Maui.
/// </summary>
public static class ShinyUnoExtensions
{
    /// <summary>
    /// Adds Shiny core services to the service collection.
    /// Call this in ConfigureServices to enable Shiny on Uno Platform.
    /// </summary>
    /// <remarks>
    /// On Android, you MUST also call AndroidShinyHost.Init(app, services) in your
    /// Application class constructor BEFORE this is called.
    /// </remarks>
    public static IServiceCollection AddShinyUno(this IServiceCollection services)
    {
        // Add platform implementation - register as both interface and concrete type
        // PushManager needs AndroidPlatform directly (not IPlatform)
        var platform = new AndroidPlatform();
        services.TryAddSingleton(platform);
        services.TryAddSingleton<IPlatform>(platform);
        return services;
    }
}
