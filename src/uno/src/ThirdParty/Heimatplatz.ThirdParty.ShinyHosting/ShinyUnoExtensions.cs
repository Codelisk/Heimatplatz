using Microsoft.Extensions.DependencyInjection;

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
        // Add platform implementation
        services.AddSingleton<IPlatform, AndroidPlatform>();
        return services;
    }
}
