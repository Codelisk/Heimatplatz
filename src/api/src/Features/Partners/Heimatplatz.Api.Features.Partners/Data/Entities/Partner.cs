using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.Partners.Data.Entities;

/// <summary>
/// Ein Partner fuer die oeffentliche /partner/-Seite (Makler-Partner, Datenquellen).
/// Wird ueber /intern/partner/ gepflegt - kein Deploy fuer neue Partner noetig
/// (gleiche Idee wie die Kontakt-Stammdaten in LegalSettings).
/// </summary>
public class Partner : BaseEntity
{
    /// <summary>Anzeigename, z.B. "Immobär Immobilien"</summary>
    public required string Name { get; set; }

    /// <summary>Kategorie - siehe PartnerCategories (Broker, DataSource)</summary>
    public required string Category { get; set; }

    /// <summary>Kurzbeschreibung fuer die Partner-Karte (2-3 Saetze)</summary>
    public string? Description { get; set; }

    /// <summary>Externe Website des Partners (http/https)</summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Logo-URL aus dem Upload-Endpoint (selbst gehostet unter /uploads/ - kein
    /// Hotlink auf Partner-Domains, die Web-CSP wuerde fremde img-src blocken).
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>Regionsangabe fuer die Karte, z.B. "Innviertel, Oberösterreich"</summary>
    public string? Region { get; set; }

    /// <summary>Jahr des Partnerschaftsbeginns ("Partner seit 2026")</summary>
    public int? PartnerSinceYear { get; set; }

    /// <summary>
    /// Verknuepfung zu Property.SourceName (z.B. "immobaer.at") fuer die
    /// Live-Inseratszahl. Null = keine automatische Zaehlung.
    /// </summary>
    public string? SourceName { get; set; }

    /// <summary>
    /// Anzeigename des Anbieters in Inseraten (Property.SellerName) - Grundlage
    /// fuer den "Inserate ansehen"-Suchlink der Web-Seite.
    /// </summary>
    public string? SellerName { get; set; }

    /// <summary>Sortierung auf der Partner-Seite (niedrig = zuerst)</summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Sichtbar auf der oeffentlichen Seite. Kein DB-Default (EF-bool-Sentinel-Falle:
    /// CLR-Init true + HasDefaultValue(true) wuerde explizites false beim Insert
    /// verschlucken) - der Wert wird immer mitgeschrieben.
    /// </summary>
    public bool IsVisible { get; set; } = true;
}
