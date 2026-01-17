using System.Text.Json.Serialization;

namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>
/// Type of property
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PropertyType
{
    /// <summary>House (single-family home, townhouse, apartment, etc.)</summary>
    House = 1,

    /// <summary>Land (building plot, farmland, etc.)</summary>
    Land = 2,

    /// <summary>Foreclosure (auction property)</summary>
    Foreclosure = 3
}
