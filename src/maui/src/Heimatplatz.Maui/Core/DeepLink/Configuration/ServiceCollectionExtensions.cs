using Microsoft.Extensions.DependencyInjection;
#if ANDROID
using Shiny.Hosting;
#endif

namespace Heimatplatz.Maui.Core.DeepLink.Configuration;

/// <summary>
/// Extension methods for registering DeepLink services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds DeepLink feature services to the service collection.
    /// Hinweis: DeepLinkService selbst wird ueber [Singleton] + AddGeneratedServices() registriert.
    /// Call from MauiProgram: builder.Services.AddDeepLinkFeature();
    /// </summary>
    public static IServiceCollection AddDeepLinkFeature(this IServiceCollection services)
    {
#if ANDROID
        // Shiny Android-Lifecycle-Hooks: OnNewIntent (App laeuft) + OnCreate (Kaltstart)
        services.AddSingleton<DeepLinkIntentHandler>();
        services.AddSingleton<IAndroidLifecycle.IOnActivityNewIntent>(sp => sp.GetRequiredService<DeepLinkIntentHandler>());
        services.AddSingleton<IAndroidLifecycle.IOnActivityOnCreate>(sp => sp.GetRequiredService<DeepLinkIntentHandler>());
#endif
        return services;
    }
}
