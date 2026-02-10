using Heimatplatz.Features.Auth.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Features.Auth.Configuration;

/// <summary>
/// DI-Registrierung fuer das Auth Feature
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert alle Services des Auth Features
    /// </summary>
    public static IServiceCollection AddAuthFeature(this IServiceCollection services)
    {
        services.AddShinyServiceRegistry();
        // ViewModels werden automatisch via [Service] Attribut registriert
        return services;
    }

    /// <summary>
    /// Registriert den globalen Auth Exception Handler fuer 401 Unauthorized Fehler.
    /// Wird im ShinyMediatorBuilder Callback aufgerufen.
    /// </summary>
    public static ShinyMediatorBuilder AddAuthExceptionHandler(this ShinyMediatorBuilder cfg)
    {
        cfg.Services.AddSingleton<IExceptionHandler, AuthExceptionHandler>();
        return cfg;
    }
}
