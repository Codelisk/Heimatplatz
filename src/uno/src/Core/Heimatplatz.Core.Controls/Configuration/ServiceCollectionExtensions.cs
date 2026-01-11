namespace Heimatplatz.Core.Controls.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddControlsFeature(this IServiceCollection services)
    {
        // Controls are registered via XAML, no additional services needed
        return services;
    }
}
