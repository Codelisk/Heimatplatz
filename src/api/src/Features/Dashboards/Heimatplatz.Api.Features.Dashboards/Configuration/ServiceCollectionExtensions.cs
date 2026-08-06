using Heimatplatz.Api.Cleanup;
using Heimatplatz.Api.Core.AiConnectorClient.Configuration;
using Heimatplatz.Api.Features.Dashboards.Jobs;
using Heimatplatz.Api.Features.Dashboards.Services;
using Heimatplatz.Api.Features.Dashboards.Services.Widgets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Dashboards.Configuration;

/// <summary>
/// DI-Registrierung fuer das Dashboards Feature ("Meine Uebersicht")
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert Handler, Widget-Resolver, KI-Designer (Provider Mock/AiConnector),
    /// DSGVO-Eraser und den Generierungs-Hintergrund-Job des Dashboards-Features.
    /// <paramref name="backgroundJobsEnabled"/> = false (keine echte Datenbank, z.B.
    /// Build-Zeit-OpenAPI-Generierung oder Integrationstests mit InMemory-Provider):
    /// statt TickerQ wird ein No-Op-Scheduler registriert.
    /// </summary>
    public static IServiceCollection AddDashboardsFeature(
        this IServiceCollection services,
        IConfiguration configuration,
        bool backgroundJobsEnabled)
    {
        services.Configure<DashboardOptions>(configuration.GetSection(DashboardOptions.SectionName));
        services.AddGeneratedServices();

        // Explizit (nicht via [Service]/TryAdd), damit IEnumerable<> alle Beitraege erhaelt.
        services.AddScoped<IUserDataEraser, DashboardsUserDataEraser>();

        // Widget-Katalog: neue Widget-Art = eine Zeile hier + Resolver-Klasse.
        // KI-Prompt-Katalog und Validator kennen sie damit automatisch.
        services.AddScoped<IDashboardWidgetResolver, StatRowWidgetResolver>();
        services.AddScoped<IDashboardWidgetResolver, PropertyListWidgetResolver>();
        services.AddScoped<IDashboardWidgetResolver, MapWidgetResolver>();
        services.AddScoped<IDashboardWidgetResolver, HighlightWidgetResolver>();
        services.AddScoped<IDashboardWidgetResolver, NewListingsWidgetResolver>();
        services.AddScoped<IDashboardWidgetResolver, PriceChartWidgetResolver>();
        services.AddScoped<IDashboardWidgetResolver, TextNoteWidgetResolver>();

        // KI-Provider je nach Konfiguration (Mock = Dev-Beispieldefinition ohne KI,
        // AiConnector = externer KI-Backend-Service)
        var options = configuration.GetSection(DashboardOptions.SectionName).Get<DashboardOptions>() ?? new DashboardOptions();
        if (string.Equals(options.Provider, "AiConnector", StringComparison.OrdinalIgnoreCase))
        {
            services.AddAiConnectorClient(configuration);
            services.AddScoped<IDashboardDesigner, AiConnectorDashboardDesigner>();
        }
        else
        {
            services.AddScoped<IDashboardDesigner, MockDashboardDesigner>();
        }

        if (backgroundJobsEnabled)
        {
            services.AddScoped<IDashboardGenerationJobScheduler, TickerQDashboardGenerationJobScheduler>();
            DashboardGenerationJob.Register(services);
        }
        else
        {
            services.AddScoped<IDashboardGenerationJobScheduler, NoOpDashboardGenerationJobScheduler>();
        }

        return services;
    }
}
