using Heimatplatz.Api.Cleanup;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.AiListing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.AiListing.Services;

/// <summary>
/// Loescht im Rahmen der Konto-Loeschung (DSGVO Art. 17) alle KI-Analyse-Jobs des
/// Benutzers - inklusive der personenbezogenen Eingaben (DictatedText, UserNotes)
/// und der hochgeladenen Medien-Dateien unter wwwroot/uploads/listings.
/// Registrierung erfolgt in <c>AddAiListingFeature</c>.
/// </summary>
public class AiListingUserDataEraser(
    AppDbContext dbContext,
    IListingMediaService mediaService,
    ILogger<AiListingUserDataEraser> logger
) : IUserDataEraser
{
    /// <summary>Nach den Properties (20) - kleinere Datenmengen, keine Abhaengigkeiten.</summary>
    public int Order => 30;

    public async Task EraseUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var mediaUrls = await dbContext.Set<ListingAnalysis>()
            .Where(a => a.UserId == userId)
            .Select(a => new { a.ImageUrls, a.VideoUrls })
            .ToListAsync(cancellationToken);

        // Dateien vor den DB-Zeilen loeschen (die Zeilen halten die Referenzen).
        // Ein Fehler pro Datei darf die Konto-Loeschung nicht blockieren.
        foreach (var url in mediaUrls.SelectMany(m => m.ImageUrls.Concat(m.VideoUrls)))
        {
            try
            {
                var path = mediaService.ResolvePhysicalPath(url);
                if (path != null)
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Upload-Datei konnte nicht geloescht werden: {Url}", url);
            }
        }

        await dbContext.Set<ListingAnalysis>()
            .Where(a => a.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
