using System.Text.Json.Serialization;

namespace Heimatplatz.Api.Features.Properties.Contracts;

/// <summary>
/// Art des Anbieters
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SellerType
{
    /// <summary>Privatperson</summary>
    Privat = 1,

    /// <summary>Immobilienmakler oder Unternehmen</summary>
    Makler = 2
}
