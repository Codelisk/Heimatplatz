using Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Requests;

/// <summary>
/// Detail einer Fehlergruppe inkl. der letzten Auftreten (Log-Eintraege).
/// </summary>
/// <param name="Id">Id der Fehlergruppe</param>
/// <param name="OccurrenceLimit">Maximale Anzahl der mitgelieferten letzten Auftreten</param>
public record GetErrorGroupDetailRequest(
    Guid Id,
    int OccurrenceLimit = 20
) : IRequest<GetErrorGroupDetailResponse>;

/// <summary>
/// Fehlergruppe mit den juengsten zugehoerigen Log-Eintraegen (neueste zuerst).
/// </summary>
public record GetErrorGroupDetailResponse(
    ErrorGroupSummaryDto Group,
    string? SampleStackTrace,
    List<TelemetryLogEntryDto> RecentOccurrences
);
