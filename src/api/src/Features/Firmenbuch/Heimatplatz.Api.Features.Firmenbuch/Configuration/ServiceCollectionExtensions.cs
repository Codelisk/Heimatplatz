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
        services.Configure<FirmenpoolOptions>(configuration.GetSection(FirmenpoolOptions.SectionName));

        services.AddHttpClient<IFirmenpoolApiClient, FirmenpoolApiClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(
                configuration.GetValue($"{FirmenpoolOptions.SectionName}:TimeoutSeconds", 60));
        })
        .AddStandardResilienceHandler();

        return services;
    }
}
