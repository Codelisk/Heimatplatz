using Heimatplatz.Api.Features.WkoCompanies.Data.Entities;

namespace Heimatplatz.Api.Features.WkoCompanies.Services;

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
}

/// <summary>Amtlicher Firmenbuch-Kurzauszug, reduziert auf die fuer WkoCompany relevanten Felder.</summary>
public record FirmenbuchAuszug
{
    /// <summary>European Unique Identifier (EUID)</summary>
    public string? Euid { get; init; }

    /// <summary>Fruehestes Vollzugsdatum (i.d.R. die Neueintragung) - amtliches Gruendungsdatum</summary>
    public DateOnly? FoundedDate { get; init; }

    public List<FirmenbuchPerson> People { get; init; } = [];
}
