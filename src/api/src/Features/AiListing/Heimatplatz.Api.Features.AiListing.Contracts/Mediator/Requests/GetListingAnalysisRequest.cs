using Heimatplatz.Api.Features.AiListing.Contracts.Enums;
using Heimatplatz.Api.Features.AiListing.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.AiListing.Contracts.Mediator.Requests;

/// <summary>
/// Fragt den Status und das Ergebnis einer KI-Inserat-Analyse ab (Polling).
/// </summary>
public record GetListingAnalysisRequest(
    Guid AnalysisId
) : IRequest<GetListingAnalysisResponse>;

/// <summary>
/// Aktueller Zustand einer Analyse. Result ist nur bei Status Finished gesetzt,
/// ErrorMessage nur bei Status Failed.
/// </summary>
public record GetListingAnalysisResponse(
    Guid AnalysisId,
    ListingAnalysisStatus Status,
    ExtractedListingData? Result,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt
);
