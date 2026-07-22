using Heimatplatz.Api.Features.Firmenbuch.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Firmenbuch.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFirmenbuchFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddGeneratedServices();

        services.Configure<FirmenbuchHvdOptions>(configuration.GetSection(FirmenbuchHvdOptions.SectionName));

        // ApiKey-Fallback auf den zentralen JustizOnline-IWG-Zugriffstoken (JustizOnline:IwgApiKey,
        // env JUSTIZONLINE_IWG_API_KEY) - der Feature-eigene Abschnitt bleibt als Override moeglich.
        services.PostConfigure<FirmenbuchHvdOptions>(o =>
        {
            if (string.IsNullOrWhiteSpace(o.ApiKey))
                o.ApiKey = configuration["JustizOnline:IwgApiKey"];
        });

        // HttpClient fuer die amtliche FBW-HVD-Schnittstelle (dokumentiertes Rate-Limit mit
        // sauberem HTTP 429 - AddStandardResilienceHandler faengt das ab)
        services.AddHttpClient<IFirmenbuchHvdClient, FirmenbuchHvdClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(
                configuration.GetValue($"{FirmenbuchHvdOptions.SectionName}:TimeoutSeconds", 30));
        })
        .AddStandardResilienceHandler();

        services.AddScoped<IFirmenbuchCatalogSyncService, FirmenbuchCatalogSyncService>();

        return services;
    }
}
