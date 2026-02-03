using Microsoft.AspNetCore.Http;

namespace Heimatplatz.Api.Features.Properties.Services;

/// <summary>
/// Service fuer das Speichern und Verwalten von Immobilien-Bildern.
/// </summary>
public interface IPropertyImageService
{
    /// <summary>
    /// Speichert hochgeladene Bilder und gibt die URLs zurueck.
    /// </summary>
    Task<List<string>> SaveImagesAsync(IReadOnlyList<IFormFile> files, CancellationToken ct = default);

    /// <summary>
    /// Loescht ein Bild anhand seiner URL.
    /// </summary>
    Task DeleteImageAsync(string imageUrl, CancellationToken ct = default);
}
