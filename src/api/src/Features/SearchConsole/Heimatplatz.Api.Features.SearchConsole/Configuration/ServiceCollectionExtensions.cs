using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.SearchConsole.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSearchConsoleFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SearchConsoleOptions>(configuration.GetSection(SearchConsoleOptions.SectionName));
        services.AddGeneratedServices();
        return services;
    }
}
