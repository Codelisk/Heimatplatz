using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Features.Properties.Mediator.Requests;
using Heimatplatz.Features.Properties.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator;

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

        // ImageUploadService benoetigt HttpClient mit BaseAddress fuer multipart Upload
        services.AddHttpClient<IImageUploadService, ImageUploadService>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
        });

        // Mediator Handler fuer Bild-Upload registrieren
        services.AddSingletonAsImplementedInterfaces<UploadPropertyImagesHandler>();

        return services;
    }
}
