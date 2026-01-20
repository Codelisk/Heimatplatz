using System.Text.Json.Serialization;

namespace Heimatplatz.Api.Features.Properties.Contracts.Enums;

/// <summary>
/// Art des Kontakts
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContactType
{
    /// <summary>Verkaeufer/Eigentuemer</summary>
    Seller = 0,

    /// <summary>Makler</summary>
    Agent = 1,

    /// <summary>Hausverwaltung</summary>
    PropertyManager = 2
}
