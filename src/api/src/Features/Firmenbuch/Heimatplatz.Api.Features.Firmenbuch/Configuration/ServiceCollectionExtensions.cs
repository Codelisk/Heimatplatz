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

        services.Configure<FirmenpoolOptions>(configuration.GetSection(FirmenpoolOptions.SectionName));

        // SyncTriggerKey-Fallback auf den historischen HVD-Abschnitt (env
        // Firmenbuch__Hvd__SyncTriggerKey) - so laeuft das bestehende Deployment ohne
        // Umkonfiguration weiter; ein Wert im Firmenpool-Abschnitt uebersteuert ihn.
        services.PostConfigure<FirmenpoolOptions>(o =>
        {
            if (string.IsNullOrWhiteSpace(o.SyncTriggerKey))
                o.SyncTriggerKey = configuration["Firmenbuch:Hvd:SyncTriggerKey"];
        });

        services.AddHttpClient<IFirmenpoolApiClient, FirmenpoolApiClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(
                configuration.GetValue($"{FirmenpoolOptions.SectionName}:TimeoutSeconds", 60));
        })
        .AddStandardResilienceHandler();

        services.AddScoped<IFirmenbuchCatalogSyncService, FirmenbuchCatalogSyncService>();

        return services;
    }
}
