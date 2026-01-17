using System.Text.Json.Serialization;

namespace Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific.Enums;

/// <summary>
/// Zoning type for land properties
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ZoningType
{
    /// <summary>Residential zone</summary>
    Residential = 1,

    /// <summary>Commercial zone</summary>
    Commercial = 2,

    /// <summary>Industrial zone</summary>
    Industrial = 3,

    /// <summary>Agricultural zone</summary>
    Agricultural = 4,

    /// <summary>Mixed use (residential and commercial)</summary>
    Mixed = 5
}
