using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Live-Kartenvorschau im Inserats-Editor: loest die gerade eingegebene Anschrift
/// zu Koordinaten auf, damit der Anbieter VOR dem Veroeffentlichen sieht, wie
/// sein Inserat auf der Karte erscheint (genau/ungefaehr/verborgen).
/// Nur fuer angemeldete Verkaeufer - es ist die eigene Adresse, die Vorschau
/// zeigt dem Eigentuemer auch die punktgenaue Variante.
/// </summary>
public record GeocodePropertyPreviewRequest(
    string City,
    string? PostalCode = null,
    string? Address = null
) : IRequest<GeocodePropertyPreviewResponse>;

/// <summary>
/// Aufgeloeste Koordinaten der Editor-Vorschau. Latitude/Longitude null =
/// Anschrift nicht aufloesbar (oder Geocoding deaktiviert) - der Editor zeigt
/// dann den Hinweis statt einer Karte.
/// </summary>
/// <param name="IsExact">True = Hausanschrift punktgenau gefunden; False = nur Ortszentrum</param>
public record GeocodePropertyPreviewResponse(
    double? Latitude,
    double? Longitude,
    bool IsExact
);
