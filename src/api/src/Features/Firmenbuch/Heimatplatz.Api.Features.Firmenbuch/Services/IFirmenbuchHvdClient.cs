using Heimatplatz.Api.Features.Firmenbuch.Data.Entities;

namespace Heimatplatz.Api.Features.Firmenbuch.Services;

public interface IFirmenbuchHvdClient
{
    /// <summary>
    /// Ob ein API-Key konfiguriert ist. Wenn false, macht <see cref="GetAuszugAsync"/> keinen
    /// HTTP-Request und liefert sofort null - der Aufrufer kann damit den ganzen
    /// Anreicherungsschritt ueberspringen statt pro Firma unnoetig zu warten.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Ruft den amtlichen Firmenbuch-Kurzauszug (AUSZUG_V2, Umfang Kurzinformation) zu einer
    /// Firmenbuchnummer ab. Liefert null wenn nicht konfiguriert, die Nummer ungueltig ist,
    /// oder die Schnittstelle einen SOAP-Fault zurueckgibt (wird geloggt).
    /// </summary>
    Task<FirmenbuchAuszug?> GetAuszugAsync(string fnr, CancellationToken ct = default);

    /// <summary>
    /// Firmensuche (SUCHEFIRMA). <paramref name="wortlaut"/> unterstuetzt Wildcards
    /// (Stern am Beginn und/oder Ende, z.B. "muster*"), <paramref name="ortNr"/> schraenkt
    /// auf Gemeinde (5-stellig), Bezirk (3-stellig) oder Bundesland (1-stellig, "4" = OOe)
    /// ein; leer = ganz Oesterreich. Die Antwort ist serverseitig auf 1000 Treffer gedeckelt
    /// (ohne Flag) - exakt 1000 Ergebnisse bedeuten daher "vermutlich abgeschnitten".
    /// Liefert null bei fehlender Konfiguration oder SOAP-Fault (wird geloggt).
    /// </summary>
    Task<List<FirmenbuchSearchResult>?> SearchAsync(string wortlaut, string ortNr = "", CancellationToken ct = default);
}

/// <summary>Ein Treffer der SUCHEFIRMA-Operation.</summary>
public record FirmenbuchSearchResult
{
    /// <summary>Firmenbuchnummer samt Pruefzeichen, normalisiert ohne Leerzeichen (z.B. "160573m")</summary>
    public required string Fnr { get; init; }

    /// <summary>Firmenname (mehrzeilige NAME-Elemente zusammengefuegt)</summary>
    public required string Name { get; init; }

    /// <summary>"" (aufrecht), "historisch" oder "geloescht" laut Schnittstelle</summary>
    public string? Status { get; init; }

    public string? Sitz { get; init; }
    public string? RechtsformCode { get; init; }
    public string? RechtsformText { get; init; }
    public string? Rechtseigenschaft { get; init; }
    public string? GerichtCode { get; init; }
    public string? GerichtText { get; init; }
}

/// <summary>Amtlicher Firmenbuch-Kurzauszug, reduziert auf die fuer WkoCompany relevanten Felder.</summary>
public record FirmenbuchAuszug
{
    /// <summary>European Unique Identifier (EUID)</summary>
    public string? Euid { get; init; }

    /// <summary>
    /// Amtliches Gruendungsdatum: Ersteintragungsdatum (DATERST) wenn vorhanden, sonst das
    /// frueheste Vollzugsdatum (bei Altfirmen nur die FBG-Ersterfassung 1994+).
    /// </summary>
    public DateOnly? FoundedDate { get; init; }

    public List<FirmenbuchPerson> People { get; init; } = [];
}
