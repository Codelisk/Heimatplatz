using Heimatplatz.Api.Features.Dashboards.Services;
using Microsoft.Extensions.DependencyInjection;
using TickerQ.DependencyInjection;

namespace Heimatplatz.Api.Features.Dashboards.Jobs;

/// <summary>
/// TickerQ-Registrierung des Dashboard-Generierungs-Jobs. Bewusst delegat-basiert
/// (MapTicker) statt ueber den [TickerFunction]-Source-Generator - die Registrierung
/// ist damit explizit und unabhaengig von Assembly-Discovery. Pro Ausfuehrung wird
/// ein eigener DI-Scope erstellt (scoped AppDbContext im
/// <see cref="DashboardGenerationProcessor"/>).
/// </summary>
public static class DashboardGenerationJob
{
    public const string FunctionName = "generate-dashboard";

    /// <summary>Anzahl Wiederholungen nach dem Erstversuch (insgesamt also MaxRetries+1 Versuche)</summary>
    public const int MaxRetries = 3;

    /// <summary>Sekunden zwischen den Versuchen (ansteigend: kurzer Haenger bis laengerer Ausfall)</summary>
    public static readonly int[] RetryIntervalsSeconds = [30, 120, 300];

    public static IServiceCollection Register(IServiceCollection services)
    {
        services.MapTicker<Guid>(FunctionName, async (context, serviceProvider, cancellationToken) =>
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<DashboardGenerationProcessor>();
            await processor.ProcessAsync(context.Request, context.RetryCount, cancellationToken);
        });
        return services;
    }
}
