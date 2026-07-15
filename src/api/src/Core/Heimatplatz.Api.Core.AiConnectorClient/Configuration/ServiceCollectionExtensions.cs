using Heimatplatz.Api.Core.AiConnectorClient.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Http;

namespace Heimatplatz.Api.Core.AiConnectorClient.Configuration;

/// <summary>
/// DI-Registrierung fuer den generierten AiConnector-OpenAPI-Client.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert den aus AiConnector.json generierten Shiny.Mediator-HTTP-Client
    /// sowie den Decorator, der den X-Api-Key Header fuer diese Requests ergaenzt.
    /// Basis-URL wird separat ueber "Mediator:Http:Heimatplatz.Api.Core.AiConnectorClient.Generated.*"
    /// konfiguriert (appsettings.json bzw. Env-Variablen).
    /// </summary>
    public static IServiceCollection AddAiConnectorClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiConnectorClientOptions>(configuration.GetSection(AiConnectorClientOptions.SectionName));

        // Shiny-Serializer VOR AddGeneratedOpenApiClient konfigurieren (wie Heimatplatz.Maui.ApiClient):
        // Der explizite DefaultJsonTypeInfoResolver erlaubt Deserialisierung der generierten
        // OpenAPI-Typen ohne eigenen JsonSerializerContext.
        services.ConfigureJsonSerializer(options =>
        {
            options.PropertyNameCaseInsensitive = true;
            options.TypeInfoResolverChain.Add(new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver());
        });
        services.AddJsonSerialization();

        services.AddGeneratedOpenApiClient();
        services.AddSingleton<IHttpRequestDecorator, AiConnectorApiKeyDecorator>();

        return services;
    }
}
