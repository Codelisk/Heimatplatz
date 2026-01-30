using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Features.Properties.Services;

/// <summary>
/// Laedt Location-Daten (Bundesland -> Bezirk -> Gemeinde) von der API.
/// Cached die Daten nach dem ersten erfolgreichen Laden.
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class LocationService(
    IMediator mediator,
    ILogger<LocationService> logger
) : ILocationService
{
    private List<LocationBundeslandDto>? _cachedLocations;
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);

    public async Task<List<LocationBundeslandDto>> GetLocationsAsync(CancellationToken ct = default)
    {
        if (_cachedLocations != null)
            return _cachedLocations;

        await _loadSemaphore.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_cachedLocations != null)
                return _cachedLocations;

            logger.LogInformation("Loading locations from API...");

            var (_, response) = await mediator.Request(
                new Heimatplatz.Core.ApiClient.Generated.GetLocationsHttpRequest(),
                ct);

            if (response?.FederalProvinces == null || response.FederalProvinces.Count == 0)
            {
                logger.LogWarning("No locations returned from API");
                return [];
            }

            _cachedLocations = response.FederalProvinces
                .Select(fp => new LocationBundeslandDto(
                    fp.Id,
                    fp.Key,
                    fp.Name,
                    fp.Districts
                        .Select(d => new LocationBezirkDto(
                            d.Id,
                            d.Key,
                            d.Name,
                            d.FederalProvinceId,
                            d.Municipalities
                                .Select(m => new LocationGemeindeDto(
                                    m.Id,
                                    m.Key,
                                    m.Name,
                                    m.PostalCode,
                                    m.Status,
                                    m.DistrictId
                                ))
                                .ToList()
                        ))
                        .ToList()
                ))
                .ToList();

            logger.LogInformation("Loaded {ProvinceCount} provinces with {DistrictCount} districts",
                _cachedLocations.Count,
                _cachedLocations.Sum(p => p.Bezirke.Count));

            return _cachedLocations;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load locations from API");
            return [];
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }

    public async Task<Guid?> ResolveMunicipalityIdAsync(string? cityName, string? postalCode, CancellationToken ct = default)
    {
        var municipalities = await GetAllMunicipalitiesAsync(ct);

        // Try exact match by PostalCode first
        if (!string.IsNullOrWhiteSpace(postalCode))
        {
            var byPostalCode = municipalities.FirstOrDefault(m =>
                m.PostalCode.Equals(postalCode.Trim(), StringComparison.OrdinalIgnoreCase));
            if (byPostalCode != null)
                return byPostalCode.Id;
        }

        // Try exact match by city name
        if (!string.IsNullOrWhiteSpace(cityName))
        {
            var trimmedCity = cityName.Trim();
            var byCityName = municipalities.FirstOrDefault(m =>
                m.Name.Equals(trimmedCity, StringComparison.OrdinalIgnoreCase));
            if (byCityName != null)
                return byCityName.Id;

            // Try partial match
            var partialMatch = municipalities.FirstOrDefault(m =>
                m.Name.Contains(trimmedCity, StringComparison.OrdinalIgnoreCase) ||
                trimmedCity.Contains(m.Name, StringComparison.OrdinalIgnoreCase));
            if (partialMatch != null)
                return partialMatch.Id;
        }

        // No match found
        return null;
    }

    public async Task<List<LocationGemeindeDto>> GetAllMunicipalitiesAsync(CancellationToken ct = default)
    {
        var locations = await GetLocationsAsync(ct);
        return locations
            .SelectMany(bl => bl.Bezirke)
            .SelectMany(bz => bz.Gemeinden)
            .ToList();
    }
}
