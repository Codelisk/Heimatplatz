using Heimatplatz.Api.Features.Properties.Contracts;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.AiListing.Contracts.Mediator.Requests;

/// <summary>
/// In-process Mediator-Request (KEIN HTTP-Endpoint): generiert eine Inserats-Beschreibung
/// aus den Eckdaten, den Stichwoertern des Verkaeufers und den Foto-URLs.
/// Aufgerufen vom Beschreibungs-Hintergrund-Job des PropertyDrafts-Features; der
/// Wortbereich ist fix im Backend konfiguriert (AiListing:Description).
/// </summary>
public class GenerateListingDescriptionRequest : IRequest<GenerateListingDescriptionResponse>
{
    public PropertyType? Type { get; set; }
    public string? Title { get; set; }
    public int? Rooms { get; set; }
    public int? LivingAreaSquareMeters { get; set; }
    public int? PlotAreaSquareMeters { get; set; }
    public int? YearBuilt { get; set; }
    public List<string>? Features { get; set; }

    /// <summary>Anzeige-Text der Gemeinde, z.B. "Linz (4020)" - fuer den Lagebezug im Text</summary>
    public string? MunicipalityDisplay { get; set; }

    /// <summary>Stichwoerter bzw. frei diktierter/getippter Text des Verkaeufers</summary>
    public string Keywords { get; set; } = "";

    /// <summary>Oeffentliche URLs der hochgeladenen Fotos (der KI-Provider kann sie abrufen)</summary>
    public List<string>? ImageUrls { get; set; }
}

/// <summary>
/// Die generierte Beschreibung (reiner Fliesstext).
/// </summary>
public record GenerateListingDescriptionResponse(
    string Description
);
