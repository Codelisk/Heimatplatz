using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.WkoCompanies.Data.Entities;

/// <summary>
/// Firma aus dem WKO Firmen-A-Z-Verzeichnis (firmen.wko.at)
/// </summary>
public class WkoCompany : BaseEntity
{
    // === Grunddaten ===

    /// <summary>Firmenname</summary>
    public required string Name { get; set; }

    /// <summary>Geschaeftsbezeichnung/Kategorie-Text (z.B. "Immobilienmakler Immobilienverwalter")</summary>
    public string? CategoryText { get; set; }

    // === Adressdaten ===

    /// <summary>Strasse und Hausnummer</summary>
    public string? Street { get; set; }

    /// <summary>Postleitzahl</summary>
    public string? PostalCode { get; set; }

    /// <summary>Ort/Stadt</summary>
    public string? City { get; set; }

    // === Kontaktdaten ===

    /// <summary>Telefonnummer(n)</summary>
    public List<string> Phones { get; set; } = [];

    /// <summary>E-Mail-Adresse</summary>
    public string? Email { get; set; }

    /// <summary>Website-URL</summary>
    public string? Website { get; set; }

    /// <summary>Freitext Oeffnungszeiten (nur von der Detailseite verfuegbar)</summary>
    public string? OpeningHoursText { get; set; }

    // === Firmendaten laut Gewerbedatenbank (nur von der Detailseite verfuegbar) ===

    /// <summary>Firmenbuchnummer</summary>
    public string? CompanyRegisterNumber { get; set; }

    /// <summary>Firmengericht</summary>
    public string? CompanyCourt { get; set; }

    /// <summary>Global Location Number</summary>
    public string? Gln { get; set; }

    /// <summary>Rechtsform (z.B. GmbH, e.U.)</summary>
    public string? LegalForm { get; set; }

    /// <summary>Gruendungsjahr (aus dem "Ueber uns"-Freitext, z.B. "2014 gegruendet")</summary>
    public int? FoundedYear { get; set; }

    /// <summary>
    /// Gruendungsdatum: fruehestes "Seit"-Datum aller Gewerbeberechtigungen. Laut WKO-Hinweis
    /// "kann vom Gruendungsdatum abweichen" - praeziser ist auf firmen.wko.at nichts verfuegbar.
    /// Grundlage fuer inkrementelle Abfragen (GetWkoCompaniesRequest.FoundedFrom).
    /// </summary>
    public DateTimeOffset? FoundedDate { get; set; }

    /// <summary>Ob die Firma als Lehrbetrieb ausgezeichnet ist</summary>
    public bool IsTrainingCompany { get; set; }

    /// <summary>Gewerbeberechtigungen (Fachgruppe, Gewerbewortlaut, gewerberechtl. Geschaeftsfuehrung, GISA-Zahl)</summary>
    public List<WkoCompanyPermit> Permits { get; set; } = [];

    // === Amtliche Firmenbuch-Daten (ueber FBW-WebServices HVD angereichert, siehe FirmenbuchHvdClient) ===

    /// <summary>European Unique Identifier laut Firmenbuch</summary>
    public string? Euid { get; set; }

    /// <summary>
    /// Amtliches Gruendungsdatum laut Firmenbuch (fruehestes Vollzugsdatum, i.d.R. die
    /// Neueintragung) - praeziser als <see cref="FoundedDate"/>, das nur aus dem
    /// Gewerbeberechtigungs-"Seit"-Datum genaehert ist.
    /// </summary>
    public DateTimeOffset? FirmenbuchFoundedDate { get; set; }

    /// <summary>Geschaeftsfuehrung/Vertretung laut Firmenbuch (Name, Geburtsdatum, Funktion)</summary>
    public List<FirmenbuchPerson> FirmenbuchManagingDirectors { get; set; } = [];

    /// <summary>
    /// Wann die Firmenbuch-Anreicherung zuletzt (erfolgreich oder erfolglos) versucht wurde.
    /// Null = noch nie versucht (z.B. weil FirmenbuchHvd:ApiKey noch nicht konfiguriert ist) -
    /// treibt die Skip-Logik im Sync (nur unversuchte Firmen bekommen einen Anreicherungs-Request).
    /// </summary>
    public DateTimeOffset? FirmenbuchEnrichedAt { get; set; }

    // === Scraping-Daten ===

    /// <summary>Eindeutige Firmaid aus firmen.wko.at (Natural Key)</summary>
    public required Guid WkoFirmaId { get; set; }

    /// <summary>Absolute URL zur Detailseite auf firmen.wko.at</summary>
    public required string DetailUrl { get; set; }

    /// <summary>Suchbegriff, ueber den die Firma zuerst gefunden wurde (z.B. "Immobilienmakler")</summary>
    public string? SourceSearchTerm { get; set; }

    /// <summary>SHA256 Hash des gescrapten Inhalts fuer Change-Detection</summary>
    public string? ContentHash { get; set; }

    /// <summary>Ob die Firma noch im WKO-Suchergebnis auftaucht</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Wann die Firma erstmals gescraped wurde</summary>
    public DateTimeOffset? FirstSeenAt { get; set; }

    /// <summary>Wann die Firma zuletzt erfolgreich gescraped wurde</summary>
    public DateTimeOffset? LastScrapedAt { get; set; }

    /// <summary>Wann die Firma aus dem WKO-Suchergebnis verschwunden ist</summary>
    public DateTimeOffset? RemovedAt { get; set; }
}
