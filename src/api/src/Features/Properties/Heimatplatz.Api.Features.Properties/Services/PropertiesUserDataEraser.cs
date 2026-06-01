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
public class PropertiesUserDataEraser(AppDbContext dbContext) : IUserDataEraser
{
    /// <summary>Nach den Notifications, da hier ggf. groessere Datenmengen betroffen sind.</summary>
    public int Order => 20;

    public async Task EraseUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // IDs der eigenen Inserate des Benutzers ermitteln
        var propertyIds = await dbContext.Set<Property>()
            .Where(p => p.UserId == userId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

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
        }
    }
}
