using System.Text.Json.Serialization;

namespace Heimatplatz.Api;

/// <summary>
/// Anbietertyp - beschreibt sowohl den registrierten Verkaeufer (User.SellerType)
/// als auch die Kategorie eines Inserats (Property.SellerType).
/// Lebt in Api.Shared, damit Auth- und Properties-Feature nicht aneinander gekoppelt sind.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SellerType
{
    /// <summary>Privatperson</summary>
    Private = 1,

    /// <summary>Makler oder Agentur</summary>
    Broker = 2,

    /// <summary>Hausverwaltung</summary>
    PropertyManager = 3
}
