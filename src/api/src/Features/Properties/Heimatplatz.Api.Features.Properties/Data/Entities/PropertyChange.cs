using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.Properties.Data.Entities;

/// <summary>
/// Journal-Eintrag fuer Immobilien-Aenderungen. Wird zentral vom
/// <see cref="PropertyChangeInterceptor"/> bei jedem SaveChanges geschrieben und
/// versorgt den Delta-Sync der Clients (GET /api/properties/changes).
/// Bewusst keine FK-Beziehung zu Property: Deleted-Eintraege (Tombstones)
/// muessen die geloeschte Immobilie ueberleben.
/// </summary>
public class PropertyChange : BaseEntity
{
    /// <summary>Id der betroffenen Immobilie</summary>
    public required Guid PropertyId { get; set; }

    /// <summary>Art der Aenderung: Created, Updated, Deleted</summary>
    public required string ChangeType { get; set; }
}

/// <summary>Konstanten fuer <see cref="PropertyChange.ChangeType"/></summary>
public static class PropertyChangeTypes
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";
}
