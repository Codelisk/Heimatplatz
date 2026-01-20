using System.Text.Json.Serialization;

namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>
/// Woher die Kontaktdaten stammen
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContactSource
{
    /// <summary>Manuell eingegeben</summary>
    Manual = 0,

    /// <summary>Von API-Import (z.B. ImmoScout24)</summary>
    Import = 1
}
