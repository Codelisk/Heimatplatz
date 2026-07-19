using Heimatplatz.Maui.Features.Telemetry.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Maui.Features.Telemetry.Configuration;

/// <summary>
/// DI-Registrierung fuer das Telemetry Feature. HeaderContributor und
/// TelemetryLogSender registriert der Shiny-DI-Generator ([Singleton]);
/// hier kommt nur der Release-Logger-Provider dazu (Debug-Laeufe wuerden
/// die Server-Telemetrie mit Entwicklungs-Warnungen fluten).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelemetryFeature(this IServiceCollection services)
    {
#if !DEBUG
        services.AddSingleton<ILoggerProvider, RemoteTelemetryLoggerProvider>();
#endif
        return services;
    }
}
