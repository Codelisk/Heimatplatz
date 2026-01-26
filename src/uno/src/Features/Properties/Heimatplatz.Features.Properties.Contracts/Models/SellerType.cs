using System.Text.Json.Serialization;

namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>
/// Type of property seller/provider
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SellerType
{
    /// <summary>Private person</summary>
    Private = 1,

    /// <summary>Real estate broker or agency</summary>
    Broker = 2,

    /// <summary>Real estate portal/platform (e.g. Willhaben, ImmoScout24)</summary>
    Portal = 3
}
