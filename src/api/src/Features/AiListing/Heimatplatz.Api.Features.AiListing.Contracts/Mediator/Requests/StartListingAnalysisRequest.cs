using Heimatplatz.Api.Features.AiListing.Contracts.Enums;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.AiListing.Contracts.Mediator.Requests;

/// <summary>
/// Startet eine KI-Analyse der hochgeladenen Medien + Diktat.
/// Die Analyse laeuft asynchron im Hintergrund; der Status kann per
/// GetListingAnalysisRequest abgefragt werden.
/// </summary>
public record StartListingAnalysisRequest(
    List<string> ImageUrls,
    List<string>? VideoUrls = null,
    string? DictatedText = null,
    string? UserNotes = null
) : IRequest<StartListingAnalysisResponse>;

/// <summary>
/// Response mit der Analyse-ID fuer das Status-Polling.
/// </summary>
public record StartListingAnalysisResponse(
    Guid AnalysisId,
    ListingAnalysisStatus Status
);
