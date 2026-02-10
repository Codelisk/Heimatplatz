using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Features.Properties.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPropertiesFeature(this IServiceCollection services, IConfiguration configuration)
    {
        // BaseAddress aus Mediator:Http Konfiguration lesen
        var httpSection = configuration.GetSection("Mediator:Http");
        var baseUrl = httpSection.GetChildren().FirstOrDefault()?.Value ?? "http://localhost:5292";

        // FilterPreferencesService benoetigt HttpClient mit BaseAddress
        services.AddHttpClient<IFilterPreferencesService, FilterPreferencesService>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
        });

        // UploadPropertyImagesHttpRequest wird automatisch via AddGeneratedOpenApiClient() registriert
        // (siehe Core.ApiClient/Configuration/ServiceCollectionExtensions.cs)

        return services;
    }
}
