using System.Net.Http.Json;
using System.Text.Json;

namespace Heimatplatz.Core.ApiClient.Manual;

/// <summary>
/// Manual HTTP client for UpdateProperty endpoint.
///
/// WORKAROUND: The Shiny Mediator OpenAPI client generator fails to generate correct code
/// for PUT/POST endpoints with both path parameters and request body.
///
/// This is a known issue tracked at: https://github.com/shinyorg/mediator/issues/54
///
/// Once the issue is resolved, this manual implementation can be replaced with the
/// generated UpdatePropertyHttpRequest from Shiny.Mediator.
/// </summary>
public class UpdatePropertyManualClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public UpdatePropertyManualClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Updates an existing property via PUT /api/properties/{id}
    /// </summary>
    /// <param name="id">Property ID from route parameter</param>
    /// <param name="request">Update data from request body</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated property response</returns>
    public async Task<UpdatePropertyResponseDto> UpdatePropertyAsync(
        Guid id,
        UpdatePropertyRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // PUT /api/properties/{id}
        var response = await _httpClient.PutAsJsonAsync(
            $"/api/properties/{id}",
            request,
            _jsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<UpdatePropertyResponseDto>(
            _jsonOptions,
            cancellationToken);

        return result ?? throw new InvalidOperationException("Failed to deserialize UpdatePropertyResponse");
    }
}
