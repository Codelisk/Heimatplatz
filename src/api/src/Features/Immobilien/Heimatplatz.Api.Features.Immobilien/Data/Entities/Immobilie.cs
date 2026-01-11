using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Immobilien.Contracts.Mediator.Requests;

namespace Heimatplatz.Api.Features.Immobilien.Data.Entities;

/// <summary>
/// Hauptentity fuer Immobilien und Grundstuecke
/// </summary>
public class Immobilie : BaseEntity
{
    /// <summary>Titel/Name der Immobilie (z.B. "Villa Poestlingberg")</summary>
    public required string Titel { get; set; }

    /// <summary>Ausfuehrliche Beschreibung</summary>
    public string? Beschreibung { get; set; }

    /// <summary>Klassifikation: Haus, Grundstueck, Wohnung</summary>
    public ImmobilienTyp Typ { get; set; }

    /// <summary>Aktueller Status: Aktiv, Reserviert, Verkauft, Inaktiv</summary>
    public ImmobilienStatus Status { get; set; } = ImmobilienStatus.Aktiv;

    /// <summary>Kaufpreis in der angegebenen Waehrung</summary>
    public decimal Preis { get; set; }

    /// <summary>Waehrungscode (ISO 4217), Standard: EUR</summary>
    public string Waehrung { get; set; } = "EUR";

    /// <summary>Wohnflaeche in m²</summary>
    public decimal Wohnflaeche { get; set; }

    /// <summary>Grundstuecksflaeche in m² (besonders relevant bei Grundstuecken)</summary>
    public decimal? Grundstuecksflaeche { get; set; }

    /// <summary>Stadt/Gemeinde</summary>
    public required string Ort { get; set; }

    /// <summary>Bezirk/Stadtteil</summary>
    public string? Bezirk { get; set; }

    /// <summary>Region/Bundesland</summary>
    public string? Region { get; set; }

    /// <summary>Laendercode (ISO 3166-1 alpha-2), Standard: AT</summary>
    public string Land { get; set; } = "AT";

    /// <summary>Geografischer Breitengrad</summary>
    public double? Breitengrad { get; set; }

    /// <summary>Geografischer Laengengrad</summary>
    public double? Laengengrad { get; set; }

    /// <summary>Anzahl der Zimmer (ohne Bad/WC)</summary>
    public int? Zimmer { get; set; }

    /// <summary>Anzahl der Schlafzimmer</summary>
    public int? Schlafzimmer { get; set; }

    /// <summary>Anzahl der Badezimmer</summary>
    public int? Badezimmer { get; set; }

    /// <summary>Baujahr des Gebaeudes</summary>
    public int? Baujahr { get; set; }

    /// <summary>Zusaetzliche Kurzinfo (z.B. "7 Zimmer", "Seezugang")</summary>
    public string? ZusatzInfo { get; set; }

    /// <summary>Bilder der Immobilie</summary>
    public ICollection<ImmobilieBild> Bilder { get; set; } = [];
}
