using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Data.Entities;

/// <summary>
/// Zwangsversteigerungs-Entity
/// </summary>
public class ForeclosureAuction : BaseEntity
{
    // === Versteigerungs-Grunddaten ===

    /// <summary>Datum und Uhrzeit der Versteigerung</summary>
    public required DateTimeOffset AuctionDate { get; set; }

    /// <summary>Kategorie der Liegenschaft</summary>
    public PropertyCategory Category { get; set; }

    /// <summary>Bezeichnung des Objekts</summary>
    public required string ObjectDescription { get; set; }

    /// <summary>Status der Versteigerung (z.B. Aktiv, Zuschlag, Verkauft)</summary>
    public string? Status { get; set; }

    // === Adressdaten ===

    /// <summary>Strasse und Hausnummer</summary>
    public required string Address { get; set; }

    /// <summary>Ort/Stadt</summary>
    public required string City { get; set; }

    /// <summary>Postleitzahl</summary>
    public required string PostalCode { get; set; }

    // === Grundbuch-Daten ===

    /// <summary>Einlagezahl (EZ) - Identifikationsnummer im Grundbuch</summary>
    public string? RegistrationNumber { get; set; }

    /// <summary>Katastralgemeinde (KG)</summary>
    public string? CadastralMunicipality { get; set; }

    /// <summary>Grundstuecksnummer (Gst.-Nr.) - Parzellennummer</summary>
    public string? PlotNumber { get; set; }

    /// <summary>Blatt-/Lfd. Nummern - Buchungsidentifikationen</summary>
    public string? SheetNumber { get; set; }

    // === Flaechendaten (in m²) ===

    /// <summary>Gesamtflaeche in m²</summary>
    public decimal? TotalArea { get; set; }

    /// <summary>Bauflaeche in m²</summary>
    public decimal? BuildingArea { get; set; }

    /// <summary>Gartenflaeche in m²</summary>
    public decimal? GardenArea { get; set; }

    /// <summary>Grundstuecksflaeche in m²</summary>
    public decimal? PlotArea { get; set; }

    // === Immobilien-Details ===

    /// <summary>Baujahr</summary>
    public int? YearBuilt { get; set; }

    /// <summary>Anzahl der Zimmer/Raeume</summary>
    public int? NumberOfRooms { get; set; }

    /// <summary>Flaechenwidmung (z.B. Bauland-Wohngebiet)</summary>
    public string? ZoningDesignation { get; set; }

    /// <summary>Gebaeudezustand/Sanierungszustand</summary>
    public string? BuildingCondition { get; set; }

    // === Versteigerungs-Details ===

    /// <summary>Geschaetzter Wert der Liegenschaft</summary>
    public decimal? EstimatedValue { get; set; }

    /// <summary>Mindestgebot</summary>
    public decimal? MinimumBid { get; set; }

    /// <summary>Besichtigungstermin(e)</summary>
    public DateTimeOffset? ViewingDate { get; set; }

    /// <summary>Gebotsfrist (bei Online-Versteigerungen)</summary>
    public DateTimeOffset? BiddingDeadline { get; set; }

    /// <summary>Anteilsquoten bei Miteigentum (z.B. "1/4")</summary>
    public string? OwnershipShare { get; set; }

    // === Rechtliche Daten ===

    /// <summary>Aktenzeichen oder Gerichtsnummer</summary>
    public string? CaseNumber { get; set; }

    /// <summary>Zustaendiges Gericht</summary>
    public string? Court { get; set; }

    /// <summary>URL zum vollstaendigen Edikt (OpenDocument)</summary>
    public string? EdictUrl { get; set; }

    /// <summary>Zusaetzliche Hinweise oder Details</summary>
    public string? Notes { get; set; }

    // === Dokumente (URLs zu externen Dateien) ===

    /// <summary>URL zu Grundrissen (PDF)</summary>
    public string? FloorPlanUrl { get; set; }

    /// <summary>URL zu Lageplan (PDF)</summary>
    public string? SitePlanUrl { get; set; }

    /// <summary>URL zu Langgutachten (PDF)</summary>
    public string? LongAppraisalUrl { get; set; }

    /// <summary>URL zu Kurzgutachten (PDF)</summary>
    public string? ShortAppraisalUrl { get; set; }
}
