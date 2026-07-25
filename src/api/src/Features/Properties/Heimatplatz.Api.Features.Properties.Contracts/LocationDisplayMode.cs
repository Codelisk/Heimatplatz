using System.Text.Json.Serialization;

namespace Heimatplatz.Api.Features.Properties.Contracts;

/// <summary>
/// Entscheidung des Anbieters, wie die Lage des Inserats auf Karten erscheint.
/// Bewusst getrennt von Property.IsLocationExact (Geocoding-Qualitaet):
/// "Exact" wird nur dann punktgenau gerendert, wenn die Adresse auch punktgenau
/// aufloesbar war - sonst faellt die Anzeige ehrlich auf den Umgebungskreis
/// zurueck (GetPropertyMapPinsHandler).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LocationDisplayMode
{
    /// <summary>Umgebungskreis mit serverseitiger Streuung (Standard fuer Nutzer-Inserate)</summary>
    Approximate = 0,

    /// <summary>Punktgenauer Pin (ZV-Edikte mit oeffentlicher Adresse; Nutzer-Opt-in)</summary>
    Exact = 1,

    /// <summary>Keine Kartenanzeige - das Inserat erscheint weder auf der Suchkarte noch mit Mini-Karte</summary>
    Hidden = 2
}
