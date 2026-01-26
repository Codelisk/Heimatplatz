using System.Text.Json.Serialization;

namespace Heimatplatz.Api.Features.Notifications.Contracts;

/// <summary>
/// Definiert den Modus fuer die Push-Notification-Filterung
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationFilterMode
{
    /// <summary>
    /// Alle neuen Immobilien - keine Filterung
    /// </summary>
    All = 0,

    /// <summary>
    /// Wie Suchfilter - nutzt die HomePage-Filtereinstellungen des Users (UserFilterPreferences)
    /// </summary>
    SameAsSearch = 1,

    /// <summary>
    /// Benutzerdefiniert - eigene Filterkriterien speziell fuer Notifications
    /// </summary>
    Custom = 2
}
