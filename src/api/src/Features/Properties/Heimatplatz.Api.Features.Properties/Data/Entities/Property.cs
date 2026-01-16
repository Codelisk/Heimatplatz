using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Properties.Contracts;

namespace Heimatplatz.Api.Features.Properties.Data.Entities;

/// <summary>
/// Immobilien-Entity
/// </summary>
public class Property : BaseEntity
{
    /// <summary>Titel/Ueberschrift der Immobilie</summary>
    public required string Titel { get; set; }

    /// <summary>Strasse und Hausnummer</summary>
    public required string Adresse { get; set; }

    /// <summary>Ort/Stadt</summary>
    public required string Ort { get; set; }

    /// <summary>Postleitzahl</summary>
    public required string Plz { get; set; }

    /// <summary>Kaufpreis in Euro</summary>
    public decimal Preis { get; set; }

    /// <summary>Wohnflaeche in m² (null bei Grundstuecken)</summary>
    public int? WohnflaecheM2 { get; set; }

    /// <summary>Grundstuecksflaeche in m²</summary>
    public int? GrundstuecksflaecheM2 { get; set; }

    /// <summary>Anzahl der Zimmer (null bei Grundstuecken)</summary>
    public int? Zimmer { get; set; }

    /// <summary>Baujahr (null bei Grundstuecken)</summary>
    public int? Baujahr { get; set; }

    /// <summary>Art der Immobilie</summary>
    public PropertyType Typ { get; set; }

    /// <summary>Art des Anbieters</summary>
    public SellerType AnbieterTyp { get; set; }

    /// <summary>Name des Anbieters</summary>
    public required string AnbieterName { get; set; }

    /// <summary>Beschreibungstext</summary>
    public string? Beschreibung { get; set; }

    /// <summary>Ausstattungsmerkmale (JSON-Array)</summary>
    public List<string> Ausstattung { get; set; } = [];

    /// <summary>Bild-URLs (JSON-Array)</summary>
    public List<string> BildUrls { get; set; } = [];

    /// <summary>ID des Benutzers (Verkaeufer), der diese Immobilie erstellt hat</summary>
    public Guid UserId { get; set; }

    /// <summary>Navigation Property zum Benutzer (Verkaeufer)</summary>
    public User User { get; set; } = null!;
}
