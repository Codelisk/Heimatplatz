using System.Text.Json.Serialization;

namespace Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific.Enums;

/// <summary>
/// Soil quality for land properties
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SoilQuality
{
    /// <summary>High quality soil, excellent for building</summary>
    High = 1,

    /// <summary>Medium quality soil, suitable for most uses</summary>
    Medium = 2,

    /// <summary>Low quality soil, may require special foundation work</summary>
    Low = 3
}
