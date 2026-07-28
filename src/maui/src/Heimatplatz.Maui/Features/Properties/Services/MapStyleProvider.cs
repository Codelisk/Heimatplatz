using System.Net.Http.Headers;
using Heimatplatz.Maui.Features.Debug.Services;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Maui.Features.Properties.Services;

public interface IMapStyleProvider
{
    Task<MapStyleResult> GetStyleAsync(bool dark, CancellationToken cancellationToken = default);
}

public sealed record MapStyleResult(string StyleJson, bool TilesAvailable);

/// <summary>
/// Liefert den Kartenstil fuer die native MapLibre-Karte: die vom Web exportierten
/// Style-JSONs (Resources/Raw/Map, erzeugt mit "npm run map-styles" in src/web)
/// mit zur Laufzeit eingesetzter Web-Origin passend zum aktiven API-Endpunkt.
/// Vor der Auswahl wird die PMTiles-Datei per Range-Request angetestet - ohne
/// erreichbare Tiles rendert der Fallback-Stil die gezeichnete Papierkarte
/// (gleiches Verhalten wie die Web-Faltkarte).
/// </summary>
public class MapStyleProvider(
    HttpClient httpClient,
    IApiEndpointService apiEndpoints,
    ILogger<MapStyleProvider> logger) : IMapStyleProvider
{
    private const string OriginPlaceholder = "https://__MAP_ORIGIN__";
    private static readonly TimeSpan ProbeCacheDuration = TimeSpan.FromMinutes(10);

    // Nur positive Ergebnisse cachen: ein voruebergehend fehlgeschlagener Probe
    // (Funkloch beim Oeffnen) soll die Karte nicht 10 Minuten im Fallback halten.
    private static readonly Dictionary<string, DateTimeOffset> _tilesOkByOrigin = [];

    public async Task<MapStyleResult> GetStyleAsync(bool dark, CancellationToken cancellationToken = default)
    {
        var origin = WebLinks.WebBaseFor(apiEndpoints.CurrentUrl);
        var tilesAvailable = await CheckTilesAvailableAsync(origin, cancellationToken);

        var assetName = $"Map/mapstyle-{(dark ? "dark" : "light")}{(tilesAvailable ? "" : "-fallback")}.json";
        using var stream = await FileSystem.OpenAppPackageFileAsync(assetName);
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync(cancellationToken);

        return new MapStyleResult(json.Replace(OriginPlaceholder, origin), tilesAvailable);
    }

    private async Task<bool> CheckTilesAvailableAsync(string origin, CancellationToken cancellationToken)
    {
        if (_tilesOkByOrigin.TryGetValue(origin, out var checkedAt) &&
            DateTimeOffset.UtcNow - checkedAt < ProbeCacheDuration)
        {
            return true;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{origin}/tiles/oberoesterreich.pmtiles");
            request.Headers.Range = new RangeHeaderValue(0, 127);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _tilesOkByOrigin[origin] = DateTimeOffset.UtcNow;
                return true;
            }

            logger.LogWarning("[MapStyle] PMTiles nicht erreichbar ({Origin}): HTTP {Status} - Fallback-Papierkarte",
                origin, (int)response.StatusCode);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "[MapStyle] PMTiles-Probe fehlgeschlagen ({Origin}) - Fallback-Papierkarte", origin);
            return false;
        }
    }
}
