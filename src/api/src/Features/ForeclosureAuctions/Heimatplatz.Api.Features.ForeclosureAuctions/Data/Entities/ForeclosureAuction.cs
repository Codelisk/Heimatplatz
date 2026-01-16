using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Data.Entities;

/// <summary>
/// Zwangsversteigerungs-Entity
/// </summary>
public class ForeclosureAuction : BaseEntity
{
    /// <summary>Datum der Versteigerung</summary>
    public required DateTimeOffset AuctionDate { get; set; }

    /// <summary>Strasse und Hausnummer</summary>
    public required string Address { get; set; }

    /// <summary>Ort/Stadt</summary>
    public required string City { get; set; }

    /// <summary>Postleitzahl</summary>
    public required string PostalCode { get; set; }

    /// <summary>Bundesland</summary>
    public AustrianState State { get; set; }

    /// <summary>Kategorie der Liegenschaft</summary>
    public PropertyCategory Category { get; set; }

    /// <summary>Bezeichnung des Objekts</summary>
    public required string ObjectDescription { get; set; }

    /// <summary>URL zum vollstaendigen Edikt (OpenDocument)</summary>
    public string? EdictUrl { get; set; }

    /// <summary>Zusaetzliche Hinweise oder Details</summary>
    public string? Notes { get; set; }

    /// <summary>Geschaetzter Wert der Liegenschaft (falls verfuegbar)</summary>
    public decimal? EstimatedValue { get; set; }

    /// <summary>Mindestgebot (falls verfuegbar)</summary>
    public decimal? MinimumBid { get; set; }

    /// <summary>Aktenzeichen oder Gerichtsnummer</summary>
    public string? CaseNumber { get; set; }

    /// <summary>Zustaendiges Gericht</summary>
    public string? Court { get; set; }
}
