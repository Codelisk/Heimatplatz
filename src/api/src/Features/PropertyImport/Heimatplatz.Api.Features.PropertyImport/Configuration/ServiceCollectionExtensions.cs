using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.PropertyImport.Configuration;

/// <summary>
/// DI-Registrierung fuer das PropertyImport Feature
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert alle Services des PropertyImport Features
    /// </summary>
    public static IServiceCollection AddPropertyImportFeature(this IServiceCollection services)
    {
        // Handler werden automatisch via [MediatorScoped] Attribut und AddMediatorRegistry() registriert

        return services;
    }
}
