namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>
/// Kategorien von Liegenschaften bei Zwangsversteigerungen
/// </summary>
public enum PropertyCategory
{
    /// <summary>Einfamilienhaus</summary>
    Einfamilienhaus = 1,

    /// <summary>Zweifamilienhaus</summary>
    Zweifamilienhaus = 2,

    /// <summary>Mehrfamilienhaus</summary>
    Mehrfamilienhaus = 3,

    /// <summary>Wohnungseigentum</summary>
    Wohnungseigentum = 4,

    /// <summary>Gewerbliche Liegenschaft</summary>
    GewerblicheLiegenschaft = 5,

    /// <summary>Grundstueck</summary>
    Grundstueck = 6,

    /// <summary>Land- und Forstwirtschaft</summary>
    LandUndForstwirtschaft = 7,

    /// <summary>Sonstiges</summary>
    Sonstiges = 8
}
