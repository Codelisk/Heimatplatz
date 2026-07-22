using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.Firmenbuch.Data.Entities;

/// <summary>
/// Ein Eintrag des lokal gespiegelten Firmenbuch-Katalogs (Quelle: SUCHEFIRMA der
/// FBW-WebServices HVD). Bewusst NICHT auf eine Branche eingeschraenkt und inklusive
/// geloeschter/historischer Firmen - der Katalog dient als Rohbestand fuer beliebige
/// spaetere Auswertungen. Eintraege werden nie geloescht, nur ueber LastSeenAt/Status
/// aktualisiert.
/// </summary>
public class FirmenbuchCompany : BaseEntity
{
    /// <summary>Firmenbuchnummer samt Pruefzeichen, normalisiert ohne Leerzeichen (Natural Key)</summary>
    public required string Fnr { get; set; }

    public required string Name { get; set; }

    /// <summary>null = aufrecht; sonst "historisch" oder "geloescht" laut Schnittstelle</summary>
    public string? Status { get; set; }

    public string? Sitz { get; set; }
    public string? RechtsformCode { get; set; }
    public string? RechtsformText { get; set; }
    public string? Rechtseigenschaft { get; set; }
    public string? GerichtCode { get; set; }
    public string? GerichtText { get; set; }

    /// <summary>ORTNR-Einschraenkung des Sync-Laufs, der den Eintrag zuletzt gesehen hat (z.B. "4" = OOe)</summary>
    public string? SourceOrtNr { get; set; }

    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
}
