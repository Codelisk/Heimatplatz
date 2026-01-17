using System.Text.Json.Serialization;

namespace Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific.Enums;

/// <summary>
/// Legal status for foreclosure properties
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LegalStatus
{
    /// <summary>Foreclosure proceedings have been initiated</summary>
    Pending = 1,

    /// <summary>Auction date has been scheduled</summary>
    Scheduled = 2,

    /// <summary>Auction is in progress</summary>
    InProgress = 3,

    /// <summary>Auction has been completed</summary>
    Completed = 4,

    /// <summary>Auction has been cancelled</summary>
    Cancelled = 5
}
