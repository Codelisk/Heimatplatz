using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific.Enums;

namespace Heimatplatz.Api.Features.OpenImmoImport.Models;

/// <summary>
/// Ergebnis eines Parser-Laufs ueber eine OpenImmo-Datei.
/// </summary>
public record OpenImmoParseResult
{
    /// <summary>Verwertbare Objekte (nur KAUF + Haus/Grundstueck, mit SourceId und Preis).</summary>
    public List<OpenImmoListing> Listings { get; init; } = [];

    /// <summary>
    /// True bei uebertragung umfang="TEIL": der Feed ist KEIN Vollbestand, der
    /// Snapshot-Delete-Pass des Syncs muss entfallen (Justimmo liefert laut Doku
    /// immer VOLL - defensiv trotzdem behandelt).
    /// </summary>
    public bool IsPartialTransfer { get; init; }

    /// <summary>SourceIds mit aktion aktionart="DELETE" (nur in TEIL-Uebertragungen relevant).</summary>
    public List<string> DeletedSourceIds { get; init; } = [];

    /// <summary>Anzahl bewusst uebersprungener Objekte (Wohnung/Gewerbe/Miete/ohne Preis/ohne Id).</summary>
    public int SkippedCount { get; init; }

    /// <summary>Hinweise zu uebersprungenen/auffaelligen Objekten (fuer Log + Status).</summary>
    public List<string> Warnings { get; init; } = [];
}

/// <summary>
/// Ein aus OpenImmo-XML geparstes, bereits produktgefiltertes Objekt
/// (nur PropertyType.House oder PropertyType.Land).
/// </summary>
public record OpenImmoListing
{
    /// <summary>openimmo_obid, Fallback objektnr_extern (Property.SourceId).</summary>
    public required string SourceId { get; init; }

    public required PropertyType Type { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    /// <summary>Strasse + Hausnummer; null wenn im Feed nicht enthalten.</summary>
    public string? Street { get; init; }

    /// <summary>
    /// verwaltung_objekt/objektadresse_freigeben: darf die genaue Adresse
    /// veroeffentlicht werden? Steuert Address-Uebernahme und LocationDisplay.
    /// </summary>
    public bool AddressReleased { get; init; }

    public string PostalCode { get; init; } = "";

    public string City { get; init; } = "";

    /// <summary>Geokoordinaten aus dem Feed (bevorzugt vor Nominatim-Geocoding).</summary>
    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public decimal Price { get; init; }

    public int? LivingAreaSquareMeters { get; init; }

    public int? PlotAreaSquareMeters { get; init; }

    public int? Rooms { get; init; }

    public int? YearBuilt { get; init; }

    public PropertyCondition? Condition { get; init; }

    /// <summary>Ausstattungs-Labels fuer Property.Features ("Keller", "Garage", ...).</summary>
    public List<string> Features { get; init; } = [];

    public bool HasGarage { get; init; }

    public bool HasGarden { get; init; }

    public bool HasBasement { get; init; }

    public int? Bedrooms { get; init; }

    public int? Bathrooms { get; init; }

    public int? Floors { get; init; }

    /// <summary>Widmung (nur Grundstuecke, aus grundstueck@grundst_typ).</summary>
    public ZoningType? Zoning { get; init; }

    public OpenImmoContact? Contact { get; init; }

    public List<OpenImmoAttachment> Attachments { get; init; } = [];

    /// <summary>verwaltung_techn/stand_vom (Property.SourceLastUpdated).</summary>
    public DateTimeOffset? StandVom { get; init; }
}

public record OpenImmoContact
{
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
}

public enum OpenImmoAttachmentMode
{
    /// <summary>location EXTERN/REMOTE mit URL in daten/pfad - wird heruntergeladen.</summary>
    ExternalUrl = 1,

    /// <summary>Base64-Inhalt direkt im XML (daten/anhanginhalt).</summary>
    Base64 = 2,

    /// <summary>location INTERN: daten/pfad referenziert eine Datei im Feed-ZIP.</summary>
    ZipEntry = 3
}

/// <summary>
/// Bild-Anhang eines Objekts. Nicht-Bild-Anhaenge (PDF-Exposes etc.) filtert
/// der Parser bereits aus.
/// </summary>
public record OpenImmoAttachment
{
    public required OpenImmoAttachmentMode Mode { get; init; }

    /// <summary>URL (ExternalUrl) bzw. ZIP-Entry-Name (ZipEntry); null bei Base64.</summary>
    public string? Location { get; init; }

    /// <summary>Roher Base64-String (nur Mode Base64); Dekodierung mit Groessencap im ImageService.</summary>
    public string? Base64Content { get; init; }

    /// <summary>True bei gruppe TITELBILD - wird als erstes Bild einsortiert.</summary>
    public bool IsTitleImage { get; init; }
}
