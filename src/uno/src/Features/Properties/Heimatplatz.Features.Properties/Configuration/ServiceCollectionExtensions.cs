using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Features.Properties.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPropertiesFeature(this IServiceCollection services)
    {
        // FilterPreferencesService is auto-registered via [Service] attribute
        // UploadPropertyImagesHttpRequest wird automatisch via AddGeneratedOpenApiClient() registriert
        // (siehe Core.ApiClient/Configuration/ServiceCollectionExtensions.cs)

        return services;
    }
}
