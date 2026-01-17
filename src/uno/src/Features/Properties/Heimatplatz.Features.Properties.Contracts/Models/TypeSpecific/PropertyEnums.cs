using System.Text.Json.Serialization;

namespace Heimatplatz.Features.Properties.Contracts.Models.TypeSpecific;

/// <summary>
/// Property condition
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PropertyCondition
{
    /// <summary>Like new, pristine condition</summary>
    LikeNew = 1,

    /// <summary>Good condition, well-maintained</summary>
    Good = 2,

    /// <summary>Average, normal wear and tear</summary>
    Average = 3,

    /// <summary>Needs renovation</summary>
    NeedsRenovation = 4
}

/// <summary>
/// Type of parking
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ParkingType
{
    /// <summary>Underground garage</summary>
    Underground = 1,

    /// <summary>Garage</summary>
    Garage = 2,

    /// <summary>Outdoor parking</summary>
    Outdoor = 3,

    /// <summary>Carport</summary>
    Carport = 4
}

/// <summary>
/// Type of garage
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GarageType
{
    /// <summary>Integrated garage in house</summary>
    Integrated = 1,

    /// <summary>Separate garage</summary>
    Separate = 2,

    /// <summary>Carport</summary>
    Carport = 3
}

/// <summary>
/// Zoning type of a plot
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ZoneType
{
    /// <summary>Residential zone</summary>
    Residential = 1,

    /// <summary>Commercial zone</summary>
    Commercial = 2,

    /// <summary>Mixed zone (residential and commercial)</summary>
    Mixed = 3,

    /// <summary>Industrial zone</summary>
    Industrial = 4
}

/// <summary>
/// Slope of a plot
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SlopeType
{
    /// <summary>Flat plot</summary>
    Flat = 1,

    /// <summary>Slight slope</summary>
    Moderate = 2,

    /// <summary>Steep slope</summary>
    Steep = 3
}

/// <summary>
/// Type of commercial use
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommercialUseType
{
    /// <summary>Office space</summary>
    Office = 1,

    /// <summary>Retail store</summary>
    Retail = 2,

    /// <summary>Warehouse, logistics</summary>
    Warehouse = 3,

    /// <summary>Production facility</summary>
    Production = 4,

    /// <summary>Restaurant, gastronomy</summary>
    Restaurant = 5,

    /// <summary>Medical practice, clinic</summary>
    Medical = 6
}
