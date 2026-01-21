using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Auth.Services;
using Microsoft.Extensions.DependencyInjection;

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
}
