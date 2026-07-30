using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Heimatplatz.Api.Features.Firmenbuch.Configuration;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.Firmenbuch.Services;

/// <summary>
/// Duenner HTTP-Client fuer die Firmenpool-API. Registrierung ausschliesslich ueber
/// AddHttpClient in der Feature-ServiceCollectionExtensions (kein DI-Attribut, sonst
/// entstuende eine zweite, konkurrierende Registrierung ohne Resilience-Handler).
/// </summary>
public class FirmenpoolApiClient(HttpClient httpClient, IOptions<FirmenpoolOptions> options)
    : IFirmenpoolApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Web;

    public async Task<FirmenpoolCompanyPage> GetCompaniesAsync(
        FirmenpoolCompanyQuery query,
        CancellationToken ct = default)
    {
        var parameters = new List<string>
        {
            $"Page={query.Page}",
            $"PageSize={query.PageSize}"
        };

        AddIfSet(parameters, "SearchText", query.SearchText);
        AddIfSet(parameters, "Sitz", query.Sitz);
        AddIfSet(parameters, "Status", query.Status);
        AddIfSet(parameters, "NameContainsAny", query.NameContainsAny);
        AddIfSet(parameters, "ExcludeFnrs", query.ExcludeFnrs);

        var url = $"{BaseUrl()}/api/firmenbuch/companies?{string.Join('&', parameters)}";

        using var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FirmenpoolCompanyPage>(JsonOptions, ct)
            ?? throw new InvalidOperationException($"Firmenpool-Antwort fuer Seite {query.Page} war leer.");
    }

    public async Task<FirmenpoolCompanyDetail?> GetCompanyDetailAsync(
        string fnr,
        CancellationToken ct = default)
    {
        var url = $"{BaseUrl()}/api/firmenbuch/companies/{Uri.EscapeDataString(fnr.Trim())}";

        using var response = await httpClient.GetAsync(url, ct);

        // Unbekannte FNR ist ein normaler Fall (Tippfehler, Firma ausserhalb OOe) -
        // kein Grund fuer eine Exception im Aufrufer.
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FirmenpoolCompanyDetail>(JsonOptions, ct);
    }

    private string BaseUrl() => options.Value.BaseUrl.TrimEnd('/');

    private static void AddIfSet(List<string> parameters, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            parameters.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
    }
}
