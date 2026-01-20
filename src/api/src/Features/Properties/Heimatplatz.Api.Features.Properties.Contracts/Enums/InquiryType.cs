using System.Text.Json.Serialization;

namespace Heimatplatz.Api.Features.Properties.Contracts.Enums;

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

    // Zukunft:
    // DirectForm = 2,   // Formular direkt an Verkaeufer
    // ExternalLink = 3  // Link zur Original-Anzeige
}
