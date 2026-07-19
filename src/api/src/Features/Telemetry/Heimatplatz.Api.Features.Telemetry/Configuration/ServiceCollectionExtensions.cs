using Heimatplatz.Api.Features.Telemetry.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Heimatplatz.Api.Features.Telemetry.Configuration;

/// <summary>
/// DI-Registrierung fuer das Telemetry Feature
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert die Auswertungs-/Ingestion-Handler und - nur mit echter Datenbank -
    /// die OpenTelemetry-Pipeline (Tracing + Logging mit eigenen DB-Prozessoren),
    /// den Batch-Writer und die Retention. <paramref name="infrastructureEnabled"/> = false
    /// (Build-Zeit-OpenAPI-Generierung, Integrationstests mit InMemory-Provider):
    /// nur die Handler bleiben aktiv, es wird nichts instrumentiert.
    /// </summary>
    public static IServiceCollection AddTelemetryFeature(
        this IServiceCollection services,
        IConfiguration configuration,
        bool infrastructureEnabled)
    {
        services.AddGeneratedServices();
        services.Configure<TelemetryOptions>(configuration.GetSection(TelemetryOptions.SectionName));
        services.AddSingleton<ErrorFingerprintService>();
        services.AddSingleton<ErrorGroupUpserter>();

        var telemetryEnabled = infrastructureEnabled
            && configuration.GetValue($"{TelemetryOptions.SectionName}:Enabled", true);
        if (!telemetryEnabled)
            return services;

        services.AddSingleton<TraceBufferService>();
        services.AddSingleton<TelemetryWriter>();
        services.AddHostedService(sp => sp.GetRequiredService<TelemetryWriter>());
        services.AddHostedService<TelemetryRetentionWorker>();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: "Heimatplatz.Api",
                serviceVersion: typeof(ServiceCollectionExtensions).Assembly.GetName().Version?.ToString(3),
                serviceInstanceId: Environment.MachineName))
            .WithTracing(tracing => tracing
                // Tail-Sampling uebernimmt der TelemetrySpanProcessor - der SDK-Sampler
                // muss alles aufzeichnen. Bewusst KEIN ParentBased: ein Client-traceparent
                // mit Flags 00 wuerde sonst die Server-Aufzeichnung abschalten.
                .SetSampler(new AlwaysOnSampler())
                .AddAspNetCoreInstrumentation(o =>
                    o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health"))
                .AddHttpClientInstrumentation()
                .AddNpgsql()
                .AddProcessor<TelemetrySpanProcessor>())
            .WithLogging(
                logging => logging.AddProcessor<TelemetryLogProcessor>(),
                options => options.IncludeFormattedMessage = true);

        return services;
    }
}
