using System.Net.Http.Headers;
using System.Net.Http.Json;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Mediator.Requests;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Features.Properties.Services;

/// <summary>
/// Service fuer das Hochladen von Immobilien-Bildern.
/// Sendet Dateien als multipart/form-data an POST /api/properties/images.
/// Registriert via AddHttpClient in ServiceCollectionExtensions.
/// </summary>
public class ImageUploadService(
    HttpClient httpClient,
    IAuthService authService,
    ILogger<ImageUploadService> logger
) : IImageUploadService
{
    private const string ApiEndpoint = "/api/properties/images";

    /// <summary>
    /// Sets the Authorization header with the current access token.
    /// </summary>
    private void SetAuthorizationHeader()
    {
        if (authService.IsAuthenticated && !string.IsNullOrEmpty(authService.AccessToken))
        {
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", authService.AccessToken);
        }
    }

    /// <inheritdoc />
    public async Task<List<string>> UploadImagesAsync(IReadOnlyList<ImageFileData> files, CancellationToken ct = default)
    {
        if (files.Count == 0)
            return [];

        try
        {
            SetAuthorizationHeader();

            using var content = new MultipartFormDataContent();

            foreach (var file in files)
            {
                var streamContent = new StreamContent(file.Stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                content.Add(streamContent, "files", file.FileName);
            }

            logger.LogInformation("Lade {Count} Bilder hoch...", files.Count);

            var response = await httpClient.PostAsync(ApiEndpoint, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Bild-Upload fehlgeschlagen: {StatusCode} - {Body}", response.StatusCode, errorBody);
                throw new HttpRequestException($"Bild-Upload fehlgeschlagen: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<UploadPropertyImagesResponse>(cancellationToken: ct)
                ?? throw new InvalidOperationException("Ungueltiges Upload-Response.");

            logger.LogInformation("{Count} Bilder erfolgreich hochgeladen.", result.ImageUrls.Count);

            return result.ImageUrls;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error uploading images");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading images");
            throw;
        }
    }
}
