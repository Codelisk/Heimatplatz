using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
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
            request.GetExcludedSellerSourceIds());

        var total = await query.CountAsync(cancellationToken);

        var pinQuery = query.Where(p => p.Latitude != null && p.Longitude != null);
        var totalWithCoordinates = await pinQuery.CountAsync(cancellationToken);

        var entities = await pinQuery
            .OrderByDescending(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Take(MaxPins)
            .ToListAsync(cancellationToken);

        var baseUrl = GetPropertiesHandler.ResolveApiBaseUrl(httpContextAccessor, configuration);

        var pins = entities.Select(p =>
        {
            // Ungenaue Lagen (Privat-Inserate, Ortszentrums-Fallback) deterministisch
            // streuen: sonst stapeln sich alle Inserate eines Orts auf einem Punkt und
            // ein einzelner Pin saehe faelschlich punktgenau aus.
            var (latitude, longitude) = p.IsLocationExact
                ? (p.Latitude!.Value, p.Longitude!.Value)
                : ApplyPrivacyJitter(p.Id, p.Latitude!.Value, p.Longitude!.Value);

            var imageUrl = GetPropertiesHandler
                .ProxyImageUrls(p.ImageUrls, baseUrl, width: GetPropertiesHandler.ListThumbnailWidth)
                .FirstOrDefault();

            return new PropertyMapPinDto(
                p.Id,
                latitude,
                longitude,
                IsApproximate: !p.IsLocationExact,
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
