using Heimatplatz.Api.Features.AiListing.Contracts.Mediator.Requests;

namespace Heimatplatz.Api.Features.AiListing.Services;

/// <summary>
/// Speichert hochgeladene Inserats-Medien (Fotos und Videos) und
/// loest gespeicherte URLs in physische Dateipfade auf (fuer die KI-Analyse).
/// </summary>
public interface IListingMediaService
{
    /// <summary>
    /// Speichert Base64-Medien unter wwwroot/uploads/listings.
    /// Liefert relative URLs, getrennt nach Bildern und Videos.
    /// </summary>
    Task<(List<string> ImageUrls, List<string> VideoUrls)> SaveMediaAsync(
        IReadOnlyList<Base64MediaData> media,
        CancellationToken ct = default);

    /// <summary>
    /// Loest eine Upload-URL (relativ oder absolut) in den physischen Dateipfad auf.
    /// Liefert null, wenn die URL nicht auf ein lokales Upload-Verzeichnis zeigt.
    /// </summary>
    string? ResolvePhysicalPath(string url);
}
