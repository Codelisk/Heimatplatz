using Heimatplatz.Features.Properties.Contracts.Mediator.Requests;

namespace Heimatplatz.Features.Properties.Contracts.Interfaces;

/// <summary>
/// Service fuer das Hochladen von Immobilien-Bildern an die API.
/// Registriert via AddHttpClient in ServiceCollectionExtensions.
/// </summary>
public interface IImageUploadService
{
    /// <summary>
    /// Laedt Bilder als multipart/form-data an die API hoch.
    /// </summary>
    Task<List<string>> UploadImagesAsync(IReadOnlyList<ImageFileData> files, CancellationToken ct = default);
}
