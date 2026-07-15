namespace Heimatplatz.Api.Features.ForeclosureAuctions.Services;

public interface IForeclosureImageService
{
    /// <summary>
    /// Waehlt geeignete Originalanhaenge aus oder extrahiert bei unbrauchbaren
    /// Quellen Fotos aus dem Langgutachten.
    /// </summary>
    Task<List<string>> PrepareImageUrlsAsync(EdiktDetail detail, CancellationToken ct = default);
}
