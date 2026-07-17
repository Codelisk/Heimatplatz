using Heimatplatz.Api.Cleanup;
using Heimatplatz.Api.Features.PropertyDrafts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.PropertyDrafts.Configuration;

/// <summary>
/// DI-Registrierung fuer das PropertyDrafts Feature
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert die Handler des PropertyDrafts Features (server-seitige
    /// Inserat-Entwuerfe des Erstellungs-Wizards) und den DSGVO-Eraser.
    /// </summary>
    public static IServiceCollection AddPropertyDraftsFeature(this IServiceCollection services)
    {
        services.AddGeneratedServices();

        // Explizit (nicht via [Service]/TryAdd), damit IEnumerable<IUserDataEraser> alle Beitraege erhaelt.
        services.AddScoped<IUserDataEraser, PropertyDraftsUserDataEraser>();

        return services;
    }
}
