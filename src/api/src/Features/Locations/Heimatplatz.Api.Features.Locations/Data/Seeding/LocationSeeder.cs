using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Locations.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.Locations.Data.Seeding;

/// <summary>
/// Seeder der Location-Daten (Bundesland, Bezirke, Gemeinden) von der OpenPLZ API importiert.
/// Importiert alle 9 oesterreichischen Bundeslaender.
/// </summary>
public class LocationSeeder(
    AppDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    ILogger<LocationSeeder> logger
) : ISeeder
{
    private const string OpenPlzBaseUrl = "https://openplzapi.org/at";

    /// <summary>
    /// Vor allen Feature-Seedern ausfuehren (Properties braucht evtl. Gemeinden)
    /// </summary>
    public int Order => 1;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Idempotent: nur seeden wenn noch keine Bundeslaender vorhanden
        if (await dbContext.Set<FederalProvince>().AnyAsync(cancellationToken))
        {
            logger.LogInformation("Location data already seeded, skipping");
            return;
        }

        logger.LogInformation("Importing location data from OpenPLZ API...");

        var httpClient = httpClientFactory.CreateClient();

        // Nur Oberoesterreich importieren (Key "4")
        await ImportFederalProvinceAsync(httpClient, "4", cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Location data import completed");
    }

    private async Task ImportFederalProvinceAsync(HttpClient httpClient, string provinceKey, CancellationToken cancellationToken)
    {
        // Bundesland-Info holen
        var provinces = await httpClient.GetFromJsonAsync<List<OpenPlzFederalProvince>>(
            $"{OpenPlzBaseUrl}/FederalProvinces",
            cancellationToken);

        var provinceData = provinces?.FirstOrDefault(p => p.Key == provinceKey);
        if (provinceData == null)
        {
            logger.LogWarning("Federal province with key {Key} not found", provinceKey);
            return;
        }

        var federalProvince = new FederalProvince
        {
            Id = Guid.NewGuid(),
            Key = provinceData.Key,
            Name = provinceData.Name
        };

        dbContext.Set<FederalProvince>().Add(federalProvince);
        logger.LogInformation("Importing federal province: {Name} (Key: {Key})", federalProvince.Name, federalProvince.Key);

        // Bezirke des Bundeslandes holen (paginated)
        var districts = await GetAllPagesAsync<OpenPlzDistrict>(
            httpClient,
            $"{OpenPlzBaseUrl}/FederalProvinces/{provinceKey}/Districts",
            cancellationToken);

        logger.LogInformation("Found {Count} districts for {Province}", districts.Count, federalProvince.Name);

        foreach (var districtData in districts)
        {
            var district = new District
            {
                Id = Guid.NewGuid(),
                Key = districtData.Key,
                Code = districtData.Code,
                Name = districtData.Name,
                FederalProvinceId = federalProvince.Id
            };

            dbContext.Set<District>().Add(district);

            // Gemeinden des Bezirks holen (paginated)
            var municipalities = await GetAllPagesAsync<OpenPlzMunicipality>(
                httpClient,
                $"{OpenPlzBaseUrl}/Districts/{districtData.Key}/Municipalities",
                cancellationToken);

            logger.LogInformation("  District {Name}: {Count} municipalities", district.Name, municipalities.Count);

            foreach (var municipalityData in municipalities)
            {
                var municipality = new Municipality
                {
                    Id = Guid.NewGuid(),
                    Key = municipalityData.Key,
                    Code = municipalityData.Code,
                    Name = municipalityData.Name,
                    PostalCode = municipalityData.PostalCode ?? string.Empty,
                    Status = municipalityData.Status,
                    DistrictId = district.Id
                };

                dbContext.Set<Municipality>().Add(municipality);
            }
        }
    }

    /// <summary>
    /// Alle Seiten einer paginierten OpenPLZ API Response abrufen
    /// </summary>
    private static async Task<List<T>> GetAllPagesAsync<T>(HttpClient httpClient, string baseUrl, CancellationToken cancellationToken)
    {
        var allItems = new List<T>();
        var page = 1;
        const int pageSize = 50;

        while (true)
        {
            var url = $"{baseUrl}?page={page}&pageSize={pageSize}";
            var items = await httpClient.GetFromJsonAsync<List<T>>(url, cancellationToken);

            if (items == null || items.Count == 0)
                break;

            allItems.AddRange(items);

            if (items.Count < pageSize)
                break;

            page++;
        }

        return allItems;
    }

    // OpenPLZ API DTOs

    private record OpenPlzFederalProvince(
        [property: JsonPropertyName("key")] string Key,
        [property: JsonPropertyName("name")] string Name
    );

    private record OpenPlzDistrict(
        [property: JsonPropertyName("key")] string Key,
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("federalProvince")] OpenPlzFederalProvince? FederalProvince
    );

    private record OpenPlzMunicipality(
        [property: JsonPropertyName("key")] string Key,
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("postalCode")] string? PostalCode,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("district")] OpenPlzDistrict? District
    );
}
