using Heimatplatz.Maui.Core.Media;
using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Bestimmt, welche Bild-URL eine Detailseite je Position anzeigt, und laedt die
/// Vorschau-Varianten im Hintergrund nach.
///
/// Der Server liefert drei Varianten derselben Fotos: Thumbnail (640px, identisch mit
/// den Listen-URLs und daher nach dem Scrollen meist schon lokal), Preview (1280px,
/// Detailansicht) und das volle Display-Bild (bis 2560px, nur Vollbild-Viewer).
/// Angezeigt wird immer die beste bereits lokal vorhandene Variante; das Nachladen
/// laeuft ausschliesslich ueber den <see cref="PropertyImageCache"/>, damit kein Bild
/// gleichzeitig von der Bildkomponente und vom Cache geholt wird.
/// </summary>
[Singleton]
public sealed class PropertyDetailImageResolver(
    PropertyImageCache imageCache,
    ILogger<PropertyDetailImageResolver> logger)
{
    /// <summary>
    /// Sofort anzeigbare URLs: lokale Preview &gt; lokales Thumbnail &gt; Thumbnail-URL.
    /// Das Thumbnail als Fallback ist bewusst gewaehlt - es laedt um ein Vielfaches
    /// schneller als die Preview und wird von <see cref="UpgradeToPreviewsAsync"/>
    /// ersetzt, sobald die scharfe Variante auf der Platte liegt.
    /// </summary>
    public List<string> ResolveDisplayUrls(IReadOnlyList<string> previewUrls, IReadOnlyList<string> thumbnailUrls)
    {
        var result = new List<string>(previewUrls.Count);

        for (var i = 0; i < previewUrls.Count; i++)
        {
            var preview = previewUrls[i];
            var cachedPreview = imageCache.GetCachedOrOriginal(preview);
            if (!string.Equals(cachedPreview, preview, StringComparison.Ordinal))
            {
                // GetCachedOrOriginal hat einen lokalen Pfad geliefert
                result.Add(cachedPreview);
                continue;
            }

            var thumbnail = i < thumbnailUrls.Count ? thumbnailUrls[i] : preview;
            result.Add(imageCache.GetCachedOrOriginal(thumbnail));
        }

        return result;
    }

    /// <summary>
    /// Laedt die Vorschau-Varianten nacheinander in den Bild-Cache und meldet jede
    /// fertige Position ueber <paramref name="onResolved"/>. Bewusst sequenziell und
    /// mit der aktuell sichtbaren Position zuerst: paralleles Laden aller Fotos wuerde
    /// genau das eine Bild ausbremsen, auf das der Benutzer gerade schaut.
    /// </summary>
    public async Task UpgradeToPreviewsAsync(
        IReadOnlyList<string> previewUrls,
        int startIndex,
        Action<int, string> onResolved,
        CancellationToken cancellationToken)
    {
        foreach (var index in EnumerateFromStart(previewUrls.Count, startIndex))
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            try
            {
                var resolved = await imageCache
                    .GetOrDownloadAsync(previewUrls[index], cancellationToken)
                    .ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    return;

                onResolved(index, resolved);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "[PropertyImages] Vorschau {Index} konnte nicht geladen werden", index);
            }
        }
    }

    /// <summary>Erst das sichtbare Bild, danach der Rest von vorne.</summary>
    private static IEnumerable<int> EnumerateFromStart(int count, int startIndex)
    {
        if (startIndex >= 0 && startIndex < count)
            yield return startIndex;

        for (var i = 0; i < count; i++)
        {
            if (i != startIndex)
                yield return i;
        }
    }
}
