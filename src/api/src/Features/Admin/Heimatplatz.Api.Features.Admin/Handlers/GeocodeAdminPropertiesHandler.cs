using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Heimatplatz.Api.Features.Properties.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Admin.Handlers;

/// <summary>
/// Geocoding-Backfill fuer Bestandsinserate ohne Koordinaten (Kartenansicht im Web).
/// Laeuft synchron im Request und ist deshalb ueber Limit gedeckelt - bei der
/// Nominatim-Drossel (1 Request/Sekunde) dauern 100 Inserate ~2-3 Minuten.
/// Braucht Geocoding:Enabled=true, sonst bleibt jeder Versuch erfolglos.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin")]
public class GeocodeAdminPropertiesHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard,
    IPropertyGeocoder propertyGeocoder,
    ILogger<GeocodeAdminPropertiesHandler> logger
) : IRequestHandler<GeocodeAdminPropertiesRequest, GeocodeAdminPropertiesResponse>
{
    [MediatorHttpPost("/properties/geocode", OperationId = "GeocodeAdminProperties")]
    public async Task<GeocodeAdminPropertiesResponse> Handle(GeocodeAdminPropertiesRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var limit = Math.Clamp(request.Limit, 1, 500);

        // Neueste zuerst: die sichtbarsten Inserate bekommen ihre Pins als erste
        var candidates = await dbContext.Set<Property>()
            .Include(p => p.Municipality)
            .Where(p => p.Latitude == null)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var geocoded = 0;
        foreach (var property in candidates)
        {
            var geocodeResult = await propertyGeocoder.GeocodeAsync(
                property.Address,
                property.PostalCode ?? property.Municipality.PostalCode,
                property.Municipality.Name,
                cancellationToken);

            if (geocodeResult != null)
            {
                property.Latitude = geocodeResult.Latitude;
                property.Longitude = geocodeResult.Longitude;
                property.IsLocationExact = geocodeResult.IsExact;
                geocoded++;
            }
        }

        if (geocoded > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        var remaining = await dbContext.Set<Property>()
            .CountAsync(p => p.Latitude == null, cancellationToken);

        logger.LogInformation(
            "[Admin] Geocoding-Backfill: {Geocoded}/{Processed} aufgeloest, {Remaining} offen",
            geocoded, candidates.Count, remaining);

        return new GeocodeAdminPropertiesResponse(
            Processed: candidates.Count,
            Geocoded: geocoded,
            Failed: candidates.Count - geocoded,
            Remaining: remaining
        );
    }
}
