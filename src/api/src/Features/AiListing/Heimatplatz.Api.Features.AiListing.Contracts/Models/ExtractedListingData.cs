using Heimatplatz.Api.Features.Properties.Contracts;

namespace Heimatplatz.Api.Features.AiListing.Contracts.Models;

/// <summary>
/// Von der KI aus Fotos/Videos und Diktat extrahierte Inseratsdaten.
/// Enthaelt bewusst NUR Felder, die sinnvoll per KI befuellt werden koennen.
/// Preis, Adresse, Gemeinde und Verkaeuferdaten bleiben manuelle Eingaben.
/// </summary>
public record ExtractedListingData(
    string Title,
    string Description,
    PropertyType Type,
    int? Rooms = null,
    int? LivingAreaSquareMeters = null,
    int? PlotAreaSquareMeters = null,
    int? YearBuilt = null,
    List<string>? Features = null,
    string? Summary = null
);
