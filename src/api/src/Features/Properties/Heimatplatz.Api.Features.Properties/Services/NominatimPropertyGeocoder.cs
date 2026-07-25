using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.Properties.Services;

/// <summary>
/// Geocoding ueber die Nominatim-Such-API (OpenStreetMap). Registrierung als
/// typisierter HttpClient in ServiceCollectionExtensions (kein [Service]-Attribut,
/// siehe shiny-di-Regel fuer HttpClient-basierte Services).
/// </summary>
public class NominatimPropertyGeocoder(
    HttpClient httpClient,
    IOptions<GeocodingOptions> options,
    ILogger<NominatimPropertyGeocoder> logger
) : IPropertyGeocoder
{
    // Drossel prozessweit (statisch): die Nominatim-Policy erlaubt maximal einen
    // Request pro Sekunde - unabhaengig davon, wie viele Scopes parallel geocodieren
    // (Create-Handler und ZV-Sync koennen gleichzeitig laufen).
    private static readonly SemaphoreSlim RateGate = new(1, 1);
    private static DateTimeOffset _lastRequestAt = DateTimeOffset.MinValue;

    public async Task<PropertyGeocodeResult?> GeocodeAsync(
        string? street,
        string? postalCode,
        string city,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled || string.IsNullOrWhiteSpace(city))
            return null;

        try
        {
            // 1. Hausanschrift (punktgenau), 2. Fallback PLZ/Ort (Ortszentrum)
            if (!string.IsNullOrWhiteSpace(street))
            {
                var exact = await QueryAsync(street, postalCode, city, cancellationToken);
                if (exact != null)
                    return exact with { IsExact = true };
            }

            var approximate = await QueryAsync(street: null, postalCode, city, cancellationToken);
            return approximate == null ? null : approximate with { IsExact = false };
        }
        catch (Exception ex)
        {
            // Fehlertoleranz per Vertrag: Koordinaten fehlen dann eben, der
            // Admin-Backfill bzw. der naechste Sync versucht es erneut.
            logger.LogWarning(ex, "Geocoding fehlgeschlagen fuer {City} ({PostalCode})", city, postalCode);
            return null;
        }
    }

    private async Task<PropertyGeocodeResult?> QueryAsync(
        string? street,
        string? postalCode,
        string city,
        CancellationToken cancellationToken)
    {
        var query = new List<string> { "format=jsonv2", "limit=1", "countrycodes=at" };
        if (!string.IsNullOrWhiteSpace(street))
            query.Add($"street={Uri.EscapeDataString(street.Trim())}");
        if (!string.IsNullOrWhiteSpace(postalCode))
            query.Add($"postalcode={Uri.EscapeDataString(postalCode.Trim())}");
        query.Add($"city={Uri.EscapeDataString(city.Trim())}");

        await ThrottleAsync(cancellationToken);

        using var response = await httpClient.GetAsync($"search?{string.Join("&", query)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Geocoder antwortete {StatusCode} fuer {City}", (int)response.StatusCode, city);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = json.RootElement;
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            return null;

        // lat/lon liefert Nominatim als Strings
        var first = root[0];
        if (!TryGetCoordinate(first, "lat", out var latitude) || !TryGetCoordinate(first, "lon", out var longitude))
            return null;

        return new PropertyGeocodeResult(latitude, longitude, IsExact: false);
    }

    private static bool TryGetCoordinate(JsonElement element, string propertyName, out double value)
    {
        value = 0;
        return element.TryGetProperty(propertyName, out var property)
            && double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private async Task ThrottleAsync(CancellationToken cancellationToken)
    {
        await RateGate.WaitAsync(cancellationToken);
        try
        {
            var nextAllowed = _lastRequestAt.AddMilliseconds(options.Value.MinRequestIntervalMs);
            var wait = nextAllowed - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, cancellationToken);

            _lastRequestAt = DateTimeOffset.UtcNow;
        }
        finally
        {
            RateGate.Release();
        }
    }
}
