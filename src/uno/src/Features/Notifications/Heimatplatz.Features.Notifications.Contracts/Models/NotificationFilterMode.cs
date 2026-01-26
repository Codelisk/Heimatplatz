using System.Text.Json.Serialization;

namespace Heimatplatz.Features.Notifications.Contracts.Models;

/// <summary>
/// Defines how notification filters are applied
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationFilterMode
{
    /// <summary>
    /// Receive notifications for all new properties
    /// </summary>
    All = 0,

    /// <summary>
    /// Use the same filter settings as the search/home page
    /// </summary>
    SameAsSearch = 1,

    /// <summary>
    /// Use custom notification-specific filter settings
    /// </summary>
    Custom = 2
}
