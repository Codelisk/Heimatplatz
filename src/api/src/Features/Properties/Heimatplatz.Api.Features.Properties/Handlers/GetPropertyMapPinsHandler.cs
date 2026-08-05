using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Heimatplatz.Api.Features.Properties.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Kartenansicht: liefert alle Treffer der aktuellen Filter als leichte Pins.
/// Nutzt exakt dieselbe Filterlogik wie GetPropertiesHandler (PropertyQueryFilters),
/// damit Karte und Trefferliste nie auseinanderlaufen.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/properties")]
public class GetPropertyMapPinsHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration
) : IRequestHandler<GetPropertyMapPinsRequest, GetPropertyMapPinsResponse>
{
    // Deckel gegen ausufernde Antworten: die Karte clustert ohnehin, mehr als
    // 500 Pins bringen visuell nichts und kosten nur Payload.
    private const int MaxPins = 500;

    [MediatorHttpGet("/map-pins", OperationId = "GetPropertyMapPins")]
    public async Task<GetPropertyMapPinsResponse> Handle(GetPropertyMapPinsRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Property>()
            .Include(p => p.Municipality)
            .AsNoTracking()
            .Where(p => !p.IsHidden);

        // Detailseiten-Mini-Karte: nur das eine Inserat (Privacy-Jitter greift trotzdem)
        if (request.PropertyId.HasValue)
            query = query.Where(p => p.Id == request.PropertyId.Value);

        query = PropertyQueryFilters.ExcludeBlockedForCurrentUser(query, dbContext, httpContextAccessor);
        query = PropertyQueryFilters.ApplyCommonFilters(
            query,
            request.GetPropertyTypes(),
            request.GetSellerTypes(),
            request.GetMunicipalityIds(),
            request.CreatedAfter,
            request.PriceMin,
            request.PriceMax,
            request.AreaMin,
            request.AreaMax,
            request.RoomsMin,
            request.SearchText,
            request.GetExcludedSellerSourceIds(),
            request.IncludeNewBuildProjects);

        var total = await query.CountAsync(cancellationToken);

        // Verborgene Lagen (LocationDisplay=Hidden) erscheinen NIE auf der Karte -
        // sie zaehlen wie Inserate ohne Koordinaten ("nur in der Liste")
        var pinQuery = query.Where(p =>
            p.Latitude != null &&
            p.Longitude != null &&
            p.LocationDisplay != LocationDisplayMode.Hidden);
        var totalWithCoordinates = await pinQuery.CountAsync(cancellationToken);

        var entities = await pinQuery
            .OrderByDescending(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Take(MaxPins)
            .ToListAsync(cancellationToken);

        var baseUrl = GetPropertiesHandler.ResolveApiBaseUrl(httpContextAccessor, configuration);

        var pins = entities.Select(p =>
        {
            // Punktgenau nur, wenn der Anbieter es WILL (LocationDisplay=Exact) UND das
            // Geocoding es HERGIBT (IsLocationExact) - wer "Genau" waehlt, dessen Adresse
            // aber nur aufs Ortszentrum aufloesbar war, bekommt ehrlich den Kreis.
            // Alle anderen Lagen werden deterministisch gestreut: sonst stapeln sich die
            // Inserate eines Orts auf einem Punkt und saehen faelschlich punktgenau aus.
            var showExact = p.LocationDisplay == LocationDisplayMode.Exact && p.IsLocationExact;
            var (latitude, longitude) = showExact
                ? (p.Latitude!.Value, p.Longitude!.Value)
                : ApplyPrivacyJitter(p.Id, p.Latitude!.Value, p.Longitude!.Value);

            var imageUrl = GetPropertiesHandler
                .ProxyImageUrls(p.ImageUrls, baseUrl, width: GetPropertiesHandler.ListThumbnailWidth)
                .FirstOrDefault();

            return new PropertyMapPinDto(
                p.Id,
                latitude,
                longitude,
                IsApproximate: !showExact,
                p.Type,
                p.SellerType,
                p.Price,
                p.Title,
                p.Municipality.Name,
                p.PostalCode ?? p.Municipality.PostalCode,
                p.MunicipalityId,
                imageUrl,
                GetPropertiesHandler.ResolveAuctionDate(p)
            );
        }).ToList();

        return new GetPropertyMapPinsResponse(
            pins,
            total,
            WithoutCoordinates: total - totalWithCoordinates,
            Truncated: totalWithCoordinates > pins.Count
        );
    }

    /// <summary>
    /// Deterministischer Versatz (~150-400 m) aus der Property-Id: dieselbe Immobilie
    /// bekommt bei jedem Request denselben Punkt (kein Springen), aber nie die exakte
    /// Hausanschrift. Oeffentlich, weil per Unit-Test abgesichert.
    /// </summary>
    public static (double Latitude, double Longitude) ApplyPrivacyJitter(Guid id, double latitude, double longitude)
    {
        var bytes = id.ToByteArray();
        // Zwei stabile Pseudozufallswerte aus den Guid-Bytes
        var angleSeed = bytes[0] | (bytes[1] << 8);
        var radiusSeed = bytes[2] | (bytes[3] << 8);

        var angle = (angleSeed % 360) * Math.PI / 180.0;
        var radiusMeters = 150 + (radiusSeed % 250);

        // Meter in Grad: 1 Breitengrad ~ 111.32 km, Laengengrad schrumpft mit cos(lat)
        var deltaLatitude = radiusMeters * Math.Sin(angle) / 111_320.0;
        var deltaLongitude = radiusMeters * Math.Cos(angle) / (111_320.0 * Math.Cos(latitude * Math.PI / 180.0));

        return (latitude + deltaLatitude, longitude + deltaLongitude);
    }
}
