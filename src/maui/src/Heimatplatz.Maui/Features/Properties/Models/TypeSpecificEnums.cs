using System.Text.Json.Serialization;

namespace Heimatplatz.Maui.Features.Properties.Models;

/// <summary>
/// Zustand einer Immobilie
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PropertyCondition
{
    /// <summary>Neuwertig</summary>
    LikeNew = 1,

    /// <summary>Gut erhalten</summary>
    Good = 2,

    /// <summary>Durchschnittlich</summary>
    Average = 3,

    /// <summary>Sanierungsbeduerftig</summary>
    NeedsRenovation = 4
}

/// <summary>
/// Flaechenwidmung eines Grundstuecks
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ZoningType
{
    /// <summary>Wohngebiet</summary>
    Residential = 1,

    /// <summary>Gewerbegebiet</summary>
    Commercial = 2,

    /// <summary>Industriegebiet</summary>
    Industrial = 3,

    /// <summary>Landwirtschaft</summary>
    Agricultural = 4,

    /// <summary>Mischgebiet</summary>
    Mixed = 5
}

/// <summary>
/// Bodenqualitaet eines Grundstuecks
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SoilQuality
{
    /// <summary>Hohe Qualitaet</summary>
    High = 1,

    /// <summary>Mittlere Qualitaet</summary>
    Medium = 2,

    /// <summary>Niedrige Qualitaet</summary>
    Low = 3
}

/// <summary>
/// Rechtlicher Status einer Zwangsversteigerung
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LegalStatus
{
    /// <summary>Verfahren anhaengig</summary>
    Pending = 1,

    /// <summary>Termin angesetzt</summary>
    Scheduled = 2,

    /// <summary>Versteigerung laufend</summary>
    InProgress = 3,

    /// <summary>Abgeschlossen</summary>
    Completed = 4,

    /// <summary>Aufgehoben</summary>
    Cancelled = 5
}
