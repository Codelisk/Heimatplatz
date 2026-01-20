using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Properties.Contracts.Enums;

namespace Heimatplatz.Api.Features.Properties.Data.Entities;

/// <summary>
/// Kontaktinformationen zu einer Immobilie
/// </summary>
public class PropertyContactInfo : BaseEntity
{
    /// <summary>Zugehoerige Immobilie</summary>
    public Guid PropertyId { get; set; }

    /// <summary>Navigation zur Immobilie</summary>
    public Property Property { get; set; } = null!;

    /// <summary>Art des Kontakts (Verkaeufer, Makler, Hausverwaltung)</summary>
    public ContactType Type { get; set; }

    /// <summary>Herkunft der Kontaktdaten</summary>
    public ContactSource Source { get; set; }

    /// <summary>Name des Kontakts</summary>
    public string? Name { get; set; }

    /// <summary>E-Mail-Adresse</summary>
    public string? Email { get; set; }

    /// <summary>Telefonnummer</summary>
    public string? Phone { get; set; }

    /// <summary>URL zur Original-Anzeige (bei Import)</summary>
    public string? OriginalListingUrl { get; set; }

    /// <summary>Name der Importquelle (z.B. "ImmoScout24")</summary>
    public string? SourceName { get; set; }

    /// <summary>ID bei der Importquelle</summary>
    public string? SourceId { get; set; }

    /// <summary>Sortierreihenfolge fuer Anzeige</summary>
    public int DisplayOrder { get; set; }
}
