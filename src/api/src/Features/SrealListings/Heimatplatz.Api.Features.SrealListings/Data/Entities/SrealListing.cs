using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts;
using Heimatplatz.Api.Features.SrealListings.Contracts;

namespace Heimatplatz.Api.Features.SrealListings.Data.Entities;

/// <summary>
/// Gescraptes Immobilienangebot von sreal.at
/// </summary>
public class SrealListing : BaseEntity
{
    // === Identifikation ===

    /// <summary>Externe ID aus URL (z.B. "964-31511")</summary>
    public required string ExternalId { get; set; }

    /// <summary>Ueberschrift des Inserats</summary>
    public required string Title { get; set; }

    // === Adressdaten ===

    /// <summary>Vollstaendige Adresse (z.B. "4121 Altenfelden")</summary>
    public required string Address { get; set; }

    /// <summary>Ortsname</summary>
    public required string City { get; set; }

    /// <summary>Postleitzahl</summary>
    public required string PostalCode { get; set; }

    /// <summary>Bezirk (aus Breadcrumbs, z.B. "Rohrbach")</summary>
    public string? District { get; set; }

    /// <summary>Bundesland</summary>
    public AustrianState State { get; set; } = AustrianState.Oberoesterreich;

    // === Objektdaten ===

    /// <summary>Objekttyp (House, Land, Vacation)</summary>
    public SrealObjectType ObjectType { get; set; }

    /// <summary>Kauf- oder Mietangebot</summary>
    public string BuyingType { get; set; } = "buy";

    /// <summary>Kaufpreis (numerisch)</summary>
    public decimal? Price { get; set; }

    /// <summary>Preis-Rohtext (z.B. "460.000,00 €")</summary>
    public string? PriceText { get; set; }

    /// <summary>Provision (z.B. "3% des Kaufpreises zzgl. 20% USt.")</summary>
    public string? Commission { get; set; }

    /// <summary>Wohnflaeche in m²</summary>
    public decimal? LivingArea { get; set; }

    /// <summary>Grundflaeche in m²</summary>
    public decimal? PlotArea { get; set; }

    /// <summary>Anzahl Zimmer</summary>
    public int? Rooms { get; set; }

    /// <summary>Objektbeschreibung (Volltext)</summary>
    public string? Description { get; set; }

    // === Energieausweis ===

    /// <summary>Heizwaermeklasse (z.B. "E")</summary>
    public string? EnergyClass { get; set; }

    /// <summary>Heizwaermebedarf (z.B. "162,9 kWh/m²a")</summary>
    public string? EnergyValue { get; set; }

    /// <summary>fGEE Wert (z.B. "2.16")</summary>
    public string? FGee { get; set; }

    /// <summary>fGEE Klasse (z.B. "D")</summary>
    public string? FGeeClass { get; set; }

    // === Bilder ===

    /// <summary>Bild-URLs aus Galerie</summary>
    public List<string> ImageUrls { get; set; } = [];

    // === Quell-URL ===

    /// <summary>Volle URL der Detailseite</summary>
    public required string SourceUrl { get; set; }

    // === Makler-Kontakt ===

    /// <summary>Name des Maklers</summary>
    public string? AgentName { get; set; }

    /// <summary>Telefonnummer des Maklers</summary>
    public string? AgentPhone { get; set; }

    /// <summary>E-Mail des Maklers</summary>
    public string? AgentEmail { get; set; }

    /// <summary>Buero/Standort des Maklers</summary>
    public string? AgentOffice { get; set; }

    // === Zusatzdaten ===

    /// <summary>Infrastruktur-Daten als JSON</summary>
    public string? Infrastructure { get; set; }

    // === Scraping-Metadaten ===

    /// <summary>SHA256 Hash des gescrapten Inhalts fuer Change-Detection</summary>
    public string? ContentHash { get; set; }

    /// <summary>Ob das Inserat noch auf sreal.at aktiv ist</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Erstmalig gescrapt</summary>
    public DateTimeOffset? FirstSeenAt { get; set; }

    /// <summary>Letzter erfolgreicher Scrape</summary>
    public DateTimeOffset? LastScrapedAt { get; set; }

    /// <summary>Wann vom Portal verschwunden</summary>
    public DateTimeOffset? RemovedAt { get; set; }

    // === Navigation Properties ===

    /// <summary>Aenderungs-Verlauf</summary>
    public ICollection<SrealListingChange> Changes { get; set; } = new List<SrealListingChange>();
}
