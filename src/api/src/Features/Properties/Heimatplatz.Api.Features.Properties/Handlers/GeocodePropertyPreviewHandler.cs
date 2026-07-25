using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Services;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Editor-Kartenvorschau: geocodiert die eingegebene Anschrift on-the-fly.
/// Fehlertolerant wie ueberall (null statt Fehler) und durch die prozessweite
/// Drossel des Geocoders (1 Request/Sekunde) automatisch ratenbegrenzt -
/// der Editor debounced clientseitig zusaetzlich.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/properties")]
public class GeocodePropertyPreviewHandler(
    IPropertyGeocoder propertyGeocoder
) : IRequestHandler<GeocodePropertyPreviewRequest, GeocodePropertyPreviewResponse>
{
    [MediatorHttpPost("/geocode-preview", OperationId = "GeocodePropertyPreview", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireSeller])]
    public async Task<GeocodePropertyPreviewResponse> Handle(GeocodePropertyPreviewRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.City))
            return new GeocodePropertyPreviewResponse(null, null, false);

        var result = await propertyGeocoder.GeocodeAsync(
            request.Address,
            request.PostalCode,
            request.City.Trim(),
            cancellationToken);

        return result == null
            ? new GeocodePropertyPreviewResponse(null, null, false)
            : new GeocodePropertyPreviewResponse(result.Latitude, result.Longitude, result.IsExact);
    }
}
