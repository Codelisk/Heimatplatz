using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Enums;

namespace Heimatplatz.Api.Features.PropertyImport.Contracts.Mediator.Requests;

/// <summary>
/// DTO fuer eine einzelne Property beim Import
/// </summary>
public record ImportPropertyDto(
    // === Identifikation (fuer Upsert) ===

    /// <summary>Name des Quellsystems (z.B. "ImmoScout24", "Willhaben")</summary>
    string SourceName,

    /// <summary>Externe ID im Quellsystem</summary>
    string SourceId,

    /// <summary>Original-URL beim Quellsystem</summary>
    string? SourceUrl,

    // === Basis-Daten ===

    /// <summary>Titel der Immobilie</summary>
    string Title,

    /// <summary>Adresse (Strasse und Hausnummer)</summary>
    string Address,

    /// <summary>Stadt/Ort</summary>
    string City,

    /// <summary>Postleitzahl</summary>
    string PostalCode,

    /// <summary>Preis in Euro</summary>
    decimal Price,

    /// <summary>Immobilientyp</summary>
    PropertyType Type,

    /// <summary>Verkaeufertyp</summary>
    SellerType SellerType,

    /// <summary>Name des Verkaeufers</summary>
    string SellerName,

    // === Optionale Felder ===

    /// <summary>Beschreibung</summary>
    string? Description = null,

    /// <summary>Wohnflaeche in m2</summary>
    int? LivingAreaSquareMeters = null,

    /// <summary>Grundstuecksflaeche in m2</summary>
    int? PlotAreaSquareMeters = null,

    /// <summary>Anzahl Zimmer</summary>
    int? Rooms = null,

    /// <summary>Baujahr</summary>
    int? YearBuilt = null,

    /// <summary>Features/Ausstattung</summary>
    List<string>? Features = null,

    /// <summary>Bild-URLs</summary>
    List<string>? ImageUrls = null,

    /// <summary>Typ-spezifische Daten (House, Land, Foreclosure)</summary>
    Dictionary<string, object>? TypeSpecificData = null,

    /// <summary>Kontaktinformationen</summary>
    List<ImportContactDto>? Contacts = null,

    /// <summary>Wie funktioniert die Anfrage zu dieser Immobilie</summary>
    InquiryType InquiryType = InquiryType.ContactData
);

/// <summary>
/// Kontaktinformationen fuer Import
/// </summary>
public record ImportContactDto(
    /// <summary>Art des Kontakts</summary>
    ContactType Type,

    /// <summary>Name des Kontakts</summary>
    string? Name = null,

    /// <summary>E-Mail-Adresse</summary>
    string? Email = null,

    /// <summary>Telefonnummer</summary>
    string? Phone = null
);
