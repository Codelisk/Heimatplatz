using Heimatplatz.Api.Cleanup;
using Heimatplatz.Api.Core.Data.Seeding.Configuration;
using Heimatplatz.Api.Features.Properties.Data;
using Heimatplatz.Api.Features.Properties.Data.Seeding;
using Heimatplatz.Api.Features.Properties.Infrastructure;
using Heimatplatz.Api.Features.Properties.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Properties.Configuration;

/// <summary>
/// DI-Registrierung fuer das Properties Feature
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert alle Services des Properties Features
    /// </summary>
    public static IServiceCollection AddPropertiesFeature(this IServiceCollection services)
    {
        services.AddGeneratedServices();

        // Geocoding (Kartenansicht): HttpClient-basiert, daher explizite Registrierung
        // statt [Service]-Attribut. Opt-in ueber Geocoding:Enabled - ohne Konfiguration
        // gehen keine externen Requests raus (Tests/CI), die Drossel (1 Request/Sekunde,
        // Nominatim-Policy) sitzt im Service selbst.
        services.AddOptions<GeocodingOptions>().BindConfiguration(GeocodingOptions.SectionName);
        services.AddHttpClient<IPropertyGeocoder, NominatimPropertyGeocoder>((provider, client) =>
        {
            var geocodingOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<GeocodingOptions>>().Value;
            client.BaseAddress = new Uri(geocodingOptions.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", geocodingOptions.UserAgent);
        });

        // PropertyChange-Journal: erfasst alle Immobilien-Aenderungen zentral beim SaveChanges
        // (Basis fuer den Client-Delta-Sync via GET /api/properties/changes)
        services.AddSingleton<IInterceptor, PropertyChangeInterceptor>();
        services.AddHostedService<PropertyChangeRetentionWorker>();

        // Account-Loeschung: loescht Inserate, Favoriten und Blockierungen des Benutzers.
        // Explizit (nicht via [Service]/TryAdd), damit IEnumerable<IUserDataEraser> alle Beitraege erhaelt.
        services.AddScoped<IUserDataEraser, PropertiesUserDataEraser>();

        // Seeder registrieren
        services.AddSeeder<SellerSourceSeeder>();
        services.AddSeeder<PropertySeeder>();
        services.AddSeeder<PropertyMunicipalityFixSeeder>();
        services.AddSeeder<PropertyOriginalListingBackfillSeeder>();
        services.AddSeeder<PropertyForeclosurePriceFixSeeder>();
        services.AddSeeder<PropertyCoordinateBackfillSeeder>();
        services.AddSeeder<PropertyDescriptionRefreshSeeder>();
        services.AddSeeder<FavoriteSeeder>();
        services.AddSeeder<BlockedSeeder>();

        return services;
    }
}
