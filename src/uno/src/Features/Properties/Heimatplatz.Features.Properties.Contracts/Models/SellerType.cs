using System.Text.Json.Serialization;

namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>
/// Anbietertyp
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SellerType
{
    /// <summary>Privatperson</summary>
    Privat = 1,

    /// <summary>Makler/Immobilienfirma</summary>
    Makler = 2
}
