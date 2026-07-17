using Heimatplatz.Api.Cleanup;
using Heimatplatz.Api.Features.PropertyDrafts.Jobs;
using Heimatplatz.Api.Features.PropertyDrafts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.PropertyDrafts.Configuration;

/// <summary>
/// DI-Registrierung fuer das PropertyDrafts Feature
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert die Handler des PropertyDrafts Features (server-seitige Inserat-Entwuerfe
    /// des Erstellungs-Wizards), den DSGVO-Eraser und den Beschreibungs-Hintergrund-Job.
    /// <paramref name="backgroundJobsEnabled"/> = false (keine echte Datenbank, z.B.
    /// Build-Zeit-OpenAPI-Generierung oder Integrationstests mit InMemory-Provider):
    /// statt TickerQ wird ein No-Op-Scheduler registriert.
    /// </summary>
    public static IServiceCollection AddPropertyDraftsFeature(this IServiceCollection services, bool backgroundJobsEnabled)
    {
        services.AddGeneratedServices();

        // Explizit (nicht via [Service]/TryAdd), damit IEnumerable<IUserDataEraser> alle Beitraege erhaelt.
        services.AddScoped<IUserDataEraser, PropertyDraftsUserDataEraser>();

        if (backgroundJobsEnabled)
        {
            services.AddScoped<IDraftDescriptionJobScheduler, TickerQDraftDescriptionJobScheduler>();
            DraftDescriptionJob.Register(services);
        }
        else
        {
            services.AddScoped<IDraftDescriptionJobScheduler, NoOpDraftDescriptionJobScheduler>();
        }

        return services;
    }
}
