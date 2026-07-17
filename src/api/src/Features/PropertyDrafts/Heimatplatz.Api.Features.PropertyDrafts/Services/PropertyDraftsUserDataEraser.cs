using Heimatplatz.Api.Cleanup;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Services;
using Heimatplatz.Api.Features.PropertyDrafts.Data.Entities;
using Heimatplatz.Api.Features.PropertyDrafts.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.PropertyDrafts.Services;

/// <summary>
/// Loescht im Rahmen der Konto-Loeschung (DSGVO Art. 17) alle Inserat-Entwuerfe des
/// Benutzers inklusive der hochgeladenen Medien-Dateien.
/// Registrierung erfolgt in <c>AddPropertyDraftsFeature</c>.
/// </summary>
public class PropertyDraftsUserDataEraser(
    AppDbContext dbContext,
    IPropertyImageService imageService,
    ILogger<PropertyDraftsUserDataEraser> logger
) : IUserDataEraser
{
    /// <summary>Nach Properties (20) und AiListing (30) - keine Abhaengigkeiten.</summary>
    public int Order => 40;

    public async Task EraseUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var payloads = await dbContext.Set<PropertyDraft>()
            .Where(d => d.UserId == userId)
            .Select(d => d.PayloadJson)
            .ToListAsync(cancellationToken);

        // Dateien vor den DB-Zeilen loeschen (die Zeilen halten die Referenzen).
        // Ein Fehler pro Datei darf die Konto-Loeschung nicht blockieren.
        foreach (var payloadJson in payloads)
        {
            var data = DraftPayloadSerializer.Deserialize(payloadJson);
            foreach (var url in (data.ImageUrls ?? []).Concat(data.VideoUrls ?? []))
            {
                try
                {
                    await imageService.DeleteImageAsync(url, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Entwurfs-Medium konnte nicht geloescht werden: {Url}", url);
                }
            }
        }

        await dbContext.Set<PropertyDraft>()
            .Where(d => d.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
