using Heimatplatz.Api.Cleanup;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Properties.Services;

/// <summary>
/// Loescht alle Properties-bezogenen Daten eines Benutzers im Rahmen der Konto-Loeschung:
/// die eigenen Inserate (inkl. Kontaktinfos) sowie Favoriten und Blockierungen.
/// Beruecksichtigt auch Favoriten/Blockierungen ANDERER Nutzer auf die zu loeschenden
/// Inserate, damit keine verwaisten Verweise zurueckbleiben (FK-sicher, ohne sich auf
/// DB-Cascade zu verlassen). Registrierung erfolgt in <c>AddPropertiesFeature</c>.
/// </summary>
public class PropertiesUserDataEraser(
    AppDbContext dbContext,
    IPropertyImageService imageService
) : IUserDataEraser
{
    /// <summary>Nach den Notifications, da hier ggf. groessere Datenmengen betroffen sind.</summary>
    public int Order => 20;

    public async Task EraseUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // IDs + Bild-URLs der eigenen Inserate des Benutzers ermitteln
        var ownProperties = await dbContext.Set<Property>()
            .Where(p => p.UserId == userId)
            .Select(p => new { p.Id, p.ImageUrls })
            .ToListAsync(cancellationToken);

        var propertyIds = ownProperties.Select(p => p.Id).ToList();

        // Favoriten: eigene des Benutzers + fremde, die auf seine Inserate verweisen
        await dbContext.Set<Favorite>()
            .Where(f => f.UserId == userId || propertyIds.Contains(f.PropertyId))
            .ExecuteDeleteAsync(cancellationToken);

        // Blockierungen: eigene des Benutzers + fremde, die auf seine Inserate verweisen
        await dbContext.Set<Blocked>()
            .Where(b => b.UserId == userId || propertyIds.Contains(b.PropertyId))
            .ExecuteDeleteAsync(cancellationToken);

        if (propertyIds.Count > 0)
        {
            // Kontaktinfos der eigenen Inserate
            await dbContext.Set<PropertyContactInfo>()
                .Where(c => propertyIds.Contains(c.PropertyId))
                .ExecuteDeleteAsync(cancellationToken);

            // Eigene Inserate
            await dbContext.Set<Property>()
                .Where(p => p.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            // ExecuteDelete laeuft am ChangeTracker (und damit am PropertyChangeInterceptor)
            // vorbei - Tombstones fuer den Client-Delta-Sync deshalb manuell journalieren
            var now = DateTimeOffset.UtcNow;
            dbContext.Set<PropertyChange>().AddRange(propertyIds.Select(id => new PropertyChange
            {
                PropertyId = id,
                ChangeType = PropertyChangeTypes.Deleted,
                CreatedAt = now
            }));
            await dbContext.SaveChangesAsync(cancellationToken);

            // Hochgeladene Bilddateien mitentfernen (DSGVO - sonst bleiben sie
            // unter wwwroot/uploads oeffentlich abrufbar). Externe/gescrapte
            // URLs ignoriert DeleteImageAsync selbst.
            foreach (var imageUrl in ownProperties.SelectMany(p => p.ImageUrls))
            {
                await imageService.DeleteImageAsync(imageUrl, cancellationToken);
            }
        }
    }
}
