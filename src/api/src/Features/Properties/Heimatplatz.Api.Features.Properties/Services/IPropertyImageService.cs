using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Microsoft.AspNetCore.Http;

namespace Heimatplatz.Api.Features.Properties.Services;

/// <summary>
/// Service fuer das Speichern und Verwalten von Immobilien-Bildern.
/// </summary>
public interface IPropertyImageService
{
    /// <summary>
    /// Speichert hochgeladene Bilder (IFormFile) und gibt die relativen URLs zurueck.
    /// </summary>
    Task<List<string>> SaveImagesAsync(IReadOnlyList<IFormFile> files, CancellationToken ct = default);

    /// <summary>
    /// Speichert Base64-kodierte Bilder und gibt die relativen URLs zurueck.
    /// </summary>
    Task<List<string>> SaveBase64ImagesAsync(IReadOnlyList<Base64ImageData> images, CancellationToken ct = default);

    /// <summary>
    /// Loescht ein Bild anhand seiner URL.
    /// </summary>
    Task DeleteImageAsync(string imageUrl, CancellationToken ct = default);
}
