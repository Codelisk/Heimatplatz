using Microsoft.Extensions.DependencyInjection;
#if __ANDROID__ || __IOS__ || __MACCATALYST__
using Shiny.Infrastructure;
#endif

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
    /// Nach app.Build() muss in App.xaml.cs OnLaunched ein <c>Shiny.Hosting.Host</c>
    /// gegen den Uno-IServiceProvider gestartet werden, damit <c>Host.Current</c>
    /// und <c>Host.Lifecycle</c> verfuegbar sind.
    /// </remarks>
    public static IServiceCollection AddShinyUno(this IServiceCollection services)
    {
#if __ANDROID__ || __IOS__ || __MACCATALYST__
        // Registriert AndroidPlatform/IosPlatform + AndroidLifecycleExecutor/IosLifecycleExecutor
        // + KeyValueStores (Settings/Secure). Identisch zu dem was HostBuilder.Build()
        // intern macht; wir bauen hier aber direkt mit der Uno-DI.
        services.AddShinyCoreServices();
#endif
        return services;
    }
}
