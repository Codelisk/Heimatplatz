using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Heimatplatz.Api.Features.Properties.Data;

/// <summary>
/// Schreibt bei jedem SaveChanges Journal-Eintraege (<see cref="PropertyChange"/>) fuer
/// alle erzeugten/geaenderten/geloeschten Immobilien - unabhaengig davon, ob die Aenderung
/// aus User-Handlern, dem Import oder dem Zwangsversteigerungs-Sync stammt.
/// Kontakt-Aenderungen (<see cref="PropertyContactInfo"/>) zaehlen als Update der Immobilie.
/// Moderation via <see cref="Property.IsHidden"/> wird fuer Clients auf Deleted/Created
/// abgebildet: Ausblenden = Tombstone (Client entfernt das Inserat aus dem Cache),
/// Einblenden = Created (Client laedt es wieder ein).
/// Clients nutzen das Journal fuer den Delta-Sync ihres lokalen Caches.
/// </summary>
public sealed class PropertyChangeInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CaptureChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CaptureChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void CaptureChanges(DbContext? context)
    {
        if (context is null)
            return;

        var now = DateTimeOffset.UtcNow;
        var changes = new Dictionary<Guid, PropertyChange>();

        foreach (var entry in context.ChangeTracker.Entries<Property>())
        {
            var changeType = entry.State switch
            {
                EntityState.Added => PropertyChangeTypes.Created,
                EntityState.Modified => ResolveModifiedChangeType(entry),
                EntityState.Deleted => PropertyChangeTypes.Deleted,
                _ => null
            };

            if (changeType is not null)
                changes[entry.Entity.Id] = CreateChange(entry.Entity.Id, changeType, now);
        }

        // Kontakt-Aenderungen ohne begleitende Property-Aenderung als Updated erfassen.
        // Created/Deleted der Property selbst hat Vorrang (aussagekraeftiger fuer Clients).
        foreach (var entry in context.ChangeTracker.Entries<PropertyContactInfo>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var propertyId = entry.Entity.PropertyId;
            if (propertyId != Guid.Empty && !changes.ContainsKey(propertyId))
                changes[propertyId] = CreateChange(propertyId, PropertyChangeTypes.Updated, now);
        }

        if (changes.Count > 0)
            context.AddRange(changes.Values);
    }

    /// <summary>
    /// IsHidden-Uebergaenge als Deleted/Created abbilden: Clients kennen das Flag nicht,
    /// fuer sie verschwindet bzw. erscheint das Inserat. Sonstige Updates eines bereits
    /// ausgeblendeten Inserats (z.B. ZV-Sync) bleiben Updated - GetPropertyChangesHandler
    /// meldet sie defensiv als Deleted, weil das Live-Nachladen Hidden ausfiltert.
    /// </summary>
    private static string ResolveModifiedChangeType(EntityEntry<Property> entry)
    {
        var hidden = entry.Property(p => p.IsHidden);
        if (hidden.OriginalValue == hidden.CurrentValue)
            return PropertyChangeTypes.Updated;

        return hidden.CurrentValue
            ? PropertyChangeTypes.Deleted
            : PropertyChangeTypes.Created;
    }

    private static PropertyChange CreateChange(Guid propertyId, string changeType, DateTimeOffset now)
        => new()
        {
            PropertyId = propertyId,
            ChangeType = changeType,
            // Explizit setzen: UpdateTimestamps im AppDbContext lief zu diesem Zeitpunkt bereits
            CreatedAt = now
        };
}
