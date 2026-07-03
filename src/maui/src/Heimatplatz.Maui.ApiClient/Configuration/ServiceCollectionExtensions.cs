using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Maui.ApiClient.Configuration;

/// <summary>
/// DI-Registrierung fuer den generierten OpenAPI-Client.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert die generierten OpenAPI-HTTP-Handler fuer Shiny.Mediator.
    /// </summary>
    public static IServiceCollection AddApiClientFeature(this IServiceCollection services)
    {
        services.AddGeneratedOpenApiClient();
        return services;
    }
}
