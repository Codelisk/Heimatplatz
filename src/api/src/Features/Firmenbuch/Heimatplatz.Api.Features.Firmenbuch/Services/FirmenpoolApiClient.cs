using System.Net.Http.Json;
using System.Text.Json;
using Heimatplatz.Api.Features.Firmenbuch.Configuration;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.Firmenbuch.Services;

/// <summary>
/// Duenner HTTP-Client fuer die Firmenpool-API. Registrierung ausschliesslich ueber
/// AddHttpClient in der Feature-ServiceCollectionExtensions (kein DI-Attribut, sonst
/// entstuende eine zweite Registrierung ohne Resilience-Handler).
/// </summary>
public class FirmenpoolApiClient(HttpClient httpClient, IOptions<FirmenpoolOptions> options)
    : IFirmenpoolApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Web;

    public async Task<FirmenpoolCompanyPage> GetCompaniesAsync(
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var baseUrl = options.Value.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/api/firmenbuch/companies?Page={page}&PageSize={pageSize}";

        using var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FirmenpoolCompanyPage>(JsonOptions, ct)
            ?? throw new InvalidOperationException($"Firmenpool-Antwort fuer Seite {page} war leer.");
    }
}
