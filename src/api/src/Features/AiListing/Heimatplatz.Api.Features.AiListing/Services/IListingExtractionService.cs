using Heimatplatz.Api.Features.AiListing.Contracts.Models;

namespace Heimatplatz.Api.Features.AiListing.Services;

/// <summary>
/// Eingaben fuer eine KI-Extraktion: physische Dateipfade der Medien plus Nutzertexte.
/// </summary>
public record ListingExtractionInput(
    IReadOnlyList<string> ImagePaths,
    IReadOnlyList<string> VideoPaths,
    string? DictatedText,
    string? UserNotes
);

/// <summary>
/// Extrahiert Inseratsdaten aus Medien und Diktat.
/// Implementierungen: CliListingExtractionService (Agent-CLI am Server),
/// MockListingExtractionService (Dev-Heuristik ohne KI).
/// </summary>
public interface IListingExtractionService
{
    Task<ExtractedListingData> ExtractAsync(ListingExtractionInput input, CancellationToken ct = default);
}
