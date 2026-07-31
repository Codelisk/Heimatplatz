using Heimatplatz.Api.Features.OpenImmoImport.Infrastructure;
using Heimatplatz.Api.Features.OpenImmoImport.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.OpenImmoImport.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenImmoImportFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddGeneratedServices();

        services.Configure<OpenImmoImportOptions>(configuration.GetSection(OpenImmoImportOptions.SectionName));

        // Bild-Downloads (EXTERN-Anhaenge): Redirects prueft der Service pro Hop
        // selbst gegen die Host-Allowlist des Feeds
        services.AddHttpClient(OpenImmoImageService.HttpClientName, client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Heimatplatz-OpenImmoImport/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            MaxConnectionsPerServer = 4
        })
        .AddStandardResilienceHandler();

        services.AddScoped<IOpenImmoImageService, OpenImmoImageService>();
        services.AddScoped<IOpenImmoPropertySyncService, OpenImmoPropertySyncService>();
        services.AddScoped<IOpenImmoImportService, OpenImmoImportService>();

        // Periodischer Drop-Ordner-Scan (Konfiguration: ScanIntervalMinutes, Default aus)
        services.AddHostedService<OpenImmoImportWorker>();

        return services;
    }
}
