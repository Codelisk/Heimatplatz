using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Heimatplatz.Api.Features.Properties.Data;

/// <summary>
/// Schreibt bei jedem SaveChanges Journal-Eintraege (<see cref="PropertyChange"/>) fuer
/// alle erzeugten/geaenderten/geloeschten Immobilien - unabhaengig davon, ob die Aenderung
/// aus User-Handlern, dem Import oder dem Zwangsversteigerungs-Sync stammt.
/// Kontakt-Aenderungen (<see cref="PropertyContactInfo"/>) zaehlen als Update der Immobilie.
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
                EntityState.Modified => PropertyChangeTypes.Updated,
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

    private static PropertyChange CreateChange(Guid propertyId, string changeType, DateTimeOffset now)
        => new()
        {
            PropertyId = propertyId,
            ChangeType = changeType,
            // Explizit setzen: UpdateTimestamps im AppDbContext lief zu diesem Zeitpunkt bereits
            CreatedAt = now
        };
}
