using System.Text.Json.Serialization;

namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>
/// Bestimmt wie eine Anfrage zu einer Immobilie funktioniert
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InquiryType
{
    /// <summary>Keine Anfrage moeglich</summary>
    None = 0,

    /// <summary>Popup mit Kontaktdaten anzeigen</summary>
    ContactData = 1
}
