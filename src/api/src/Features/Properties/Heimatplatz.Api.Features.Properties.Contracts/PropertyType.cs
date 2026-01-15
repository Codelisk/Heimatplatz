using System.Text.Json.Serialization;

namespace Heimatplatz.Api.Features.Properties.Contracts;

/// <summary>
/// Art der Immobilie
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PropertyType
{
    /// <summary>Einfamilienhaus, Reihenhaus, Villa, etc.</summary>
    Haus = 1,

    /// <summary>Baugrund, Grundstueck</summary>
    Grundstueck = 2,

    /// <summary>Immobilie aus Zwangsversteigerung</summary>
    Zwangsversteigerung = 3
}
